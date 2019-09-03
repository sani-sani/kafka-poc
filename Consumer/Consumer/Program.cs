using System;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kafka.Client.Messages;
using log4net.Config;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            string topic = ConfigurationManager.AppSettings["Topic"];
            int timeToExecuteInMilliSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeToExecuteInMilliSeconds"]);
            int numberOfConsumers = Convert.ToInt32(ConfigurationManager.AppSettings["NumberOfConsumers"]);
            
            Task<IEnumerable<Message>>[] taskArray = new Task<IEnumerable<Message>>[numberOfConsumers];

            for (int i = 0; i < numberOfConsumers; i++)
            {
                taskArray[i] = Task.Factory.StartNew(() =>
                    {
                        Consumer consumer = new Consumer();
                        return consumer.ConsumeRecords(topic, timeToExecuteInMilliSeconds);
                    });
            }

            Task.WaitAll(taskArray);

            int totalMessagesConsumed = 0;
            for (int i = 0; i < taskArray.Length; i++)
            {
                var messages = taskArray[i].Result;
                if (messages != null)
                {
                    Console.WriteLine("Consumer{0} consumed {1} messages", i, messages.Count());
                    totalMessagesConsumed = totalMessagesConsumed + messages.Count();
                }
            }

            Console.WriteLine("Total messages consumed by all consumers is {0}", totalMessagesConsumed);

            Console.ReadLine();
        }
    }
}
