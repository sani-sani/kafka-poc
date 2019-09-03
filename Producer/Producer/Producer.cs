using Kafka.Client.Cfg;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Kafka.Client;
using Kafka.Client.Producers;
using Kafka.Client.Messages;

namespace Producer
{
    public class Producer
    {
        int numberOfRecordsProcessed = 0;
        Stopwatch watch;

        public int ProduceMessages(string topic, IEnumerable<ProducerData<string, Message>> batchMessages, int timeToExecuteInMilliSeconds)
        {
            watch = Stopwatch.StartNew();
            var brokers = GetBrokerConfigurations();
            var producerConfiguration = new ProducerConfiguration(brokers);
            producerConfiguration.ClientId = "xyz-123-abc";
            while (watch.ElapsedMilliseconds <= timeToExecuteInMilliSeconds)
            {
                using (var kafkaProducer = new Kafka.Client.Producers.Producer(producerConfiguration))
                {
                    kafkaProducer.Send(batchMessages);
                    numberOfRecordsProcessed = numberOfRecordsProcessed + batchMessages.Count();
                }
            }
            watch.Stop();
            Console.WriteLine("{0} messages produced in {1} milliseconds. Thread ID: {2}", numberOfRecordsProcessed, watch.ElapsedMilliseconds, System.Threading.Thread.CurrentThread.ManagedThreadId);
            return numberOfRecordsProcessed;
        }

        private IList<BrokerConfiguration> GetBrokerConfigurations()
        {
            List<BrokerConfiguration> brokerConfigurations = new List<BrokerConfiguration>();
            string brokers = ConfigurationManager.AppSettings["Brokers"];
            string[] brokerArray = brokers.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            int port = String.IsNullOrEmpty(ConfigurationManager.AppSettings["BrokerPort"]) ? 8080 : Convert.ToInt32(ConfigurationManager.AppSettings["BrokerPort"]);
            if (brokerArray.Count() <= 0)
                Console.WriteLine("No brokers found!");

            var broker = default(BrokerConfiguration);
            for (int i = 0; i < brokerArray.Length; i++)
            {
                broker = new BrokerConfiguration()
                {
                    //BrokerId = i + 1,
                    Host = brokerArray[i].Trim(),
                    Port = port
                };
                brokerConfigurations.Add(broker);
            }

            return brokerConfigurations;
        }
    }
}
