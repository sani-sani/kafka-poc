using Kafka.Client.Cfg;
using Kafka.Client.Consumers;
using Kafka.Client.Helper;
using Kafka.Client.Messages;
using Kafka.Client.Requests;
using Kafka.Client.Serialization;
using log4net.Config;
using CassandraHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer
{
    public class Consumer
    {
        CassandraDbContext _dbContext = new CassandraDbContext(ConfigurationManager.ConnectionStrings["CassandraConnString"].ConnectionString);
        CancellationTokenSource _tokenSource = new CancellationTokenSource();
        Timer _timer = null;
        Stopwatch _watch;
        string _consumerGroupId = ConfigurationManager.AppSettings["ConsumerGroupId"];
        string _consumerUniqueId = ConfigurationManager.AppSettings["ConsumerUniqueId"];
        string _zooKeeperString = ConfigurationManager.AppSettings["ZookeeperString"];
        bool _isCassandraAsync = Convert.ToBoolean(ConfigurationManager.AppSettings["IsCassandraInsertionAsync"]);
        bool _enablePerFetchOffsetCommit = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePerFetchOffsetCommit"]);
        bool _enableBatchOffsetCommit = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableBatchOffsetCommit"]);
        int _offsetCommitBatchCount = Convert.ToInt32(ConfigurationManager.AppSettings["OffsetCommitBatchCount"]);

        public IEnumerable<Message> ConsumeRecords(string topic, int timeToExecuteInMilliSeconds)
        {
            XmlConfigurator.Configure();
            CancellationToken token = _tokenSource.Token;
            _timer = new Timer(TimerElapsed, null, timeToExecuteInMilliSeconds, timeToExecuteInMilliSeconds);
            _watch = Stopwatch.StartNew();
            ConsumerConfiguration config = new ConsumerConfiguration
              {
                  AutoCommit = false,
                  GroupId = _consumerGroupId,
                  ConsumerId = _consumerUniqueId + Thread.CurrentThread.ManagedThreadId,
                  MaxFetchBufferLength = 20000,
                  //FetchSize = fetchSize,
                  AutoOffsetReset = OffsetRequest.SmallestTime,
                  NumberOfTries = 20,
                  ZooKeeper = new ZooKeeperConfiguration(_zooKeeperString, 30000, 30000, 8000),
                  Verbose = true
              };
            var balancedConsumer = new ZookeeperConsumerConnector(config, true);
            // grab streams for desired topics 
            Dictionary<string, int> topicMap = new Dictionary<string, int>();
            topicMap.Add(topic, 1);
            var streams = balancedConsumer.CreateMessageStreams(topicMap, new DefaultDecoder());
            var kafkaMessageStream = streams[topic][0];

            List<Message> consumedMessages = new List<Message>();
            try
            {
                foreach (Message message in kafkaMessageStream.GetCancellable(token))
                {
                    var insertQuery = CqlQuery.InsertInto(ConfigurationManager.AppSettings["CassandraTable"])
                               .SetValue("kafkaId", new Random().Next())
                               .SetValue("NGMData", Encoding.UTF8.GetString(message.Payload))
                               .SetValue("timeConsumed", timeToExecuteInMilliSeconds);
                    var task = _dbContext.ExecuteNonQueryAsync(insertQuery);
                    if (_enablePerFetchOffsetCommit)
                        balancedConsumer.CommitOffset(topic, Convert.ToInt32(message.PartitionId), message.Offset);
                    if (!_isCassandraAsync)
                        task.Wait();
                    consumedMessages.Add(message);
                    if (_watch.ElapsedMilliseconds >= timeToExecuteInMilliSeconds)
                        break;

                    if (_enableBatchOffsetCommit && (consumedMessages.Count() ^ _offsetCommitBatchCount) == 0)
                        balancedConsumer.CommitOffsets();                        
                }
            }
            catch (OperationCanceledException operationCancelledException) { }
            return consumedMessages;
        }

        private void TimerElapsed(object obj)
        {
            _tokenSource.Cancel();
        }
    }
}
