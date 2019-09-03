using Kafka.Client.Messages;
using Kafka.Client.Producers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kafka.Client.Producers;

namespace Producer
{
    class Program
    {
        static void Main(string[] args)
        {
            var batchMessages = GetBatchMessages();
            string topic = ConfigurationManager.AppSettings["Topic"];
            int timeToExecuteInMilliSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeToExecuteInMilliSeconds"]);
            int numberOfProducers = Convert.ToInt32(ConfigurationManager.AppSettings["NumberOfProducers"]);

            Task<int>[] taskArray = new Task<int>[numberOfProducers];

            for (int i = 0; i < numberOfProducers; i++)
            {
                taskArray[i] = Task.Factory.StartNew(() =>
                    {
                        Producer producer = new Producer();
                        return producer.ProduceMessages(topic, batchMessages, timeToExecuteInMilliSeconds);
                    });
            }

            Task.WaitAll(taskArray);

            int totalRecordsProcessed = 0;
            foreach (var task in taskArray)
                totalRecordsProcessed = totalRecordsProcessed + task.Result;

            Console.WriteLine("Total records produced: {0}", totalRecordsProcessed);

            Console.ReadLine();
        }

        private static IEnumerable<ProducerData<string,Message>> GetBatchMessages()
        {
            string content = String.Empty;
            int batchSize = Convert.ToInt32(ConfigurationManager.AppSettings["BatchSize"]);
            string contentFileName = ConfigurationManager.AppSettings["ContentFileName"];
            string topic = ConfigurationManager.AppSettings["Topic"];

            using (StreamReader reader = new StreamReader(contentFileName))
                content = reader.ReadToEnd();

            byte[] contentInBytes = Encoding.UTF8.GetBytes(content);

            List<ProducerData<string, Message>> batchMessages = new List<ProducerData<string, Message>>();
            for (int i = 0; i < batchSize; i++)
                batchMessages.Add(new ProducerData<string, Message>(topic, new Message(contentInBytes)));
            
            return batchMessages;
        }
    }
}
