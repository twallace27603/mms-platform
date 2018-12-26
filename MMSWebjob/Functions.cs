using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using MMSLibrary.Queue;
using MMSLibrary.Table;
using Newtonsoft.Json;
using MMSLibrary;

namespace MMSWebjob
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger(QueueProcessor.QueueName)] string message, TextWriter log)
        {
            var connection = System.Configuration.ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString;
            var processor = new TableProcessor(connection);
            var item = JsonConvert.DeserializeObject<DatagramItem>(message);
            Task t = processor.AddItem(item);
            t.Wait();
            log.WriteLine($"Added table row for {item.BatchId} - {item.ID}");
        }
    }
}
