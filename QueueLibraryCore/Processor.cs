using System;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QueueLibraryCore
{
    public class Processor
    {
        public static string QueueName = "process";
        public static string ConnectionName = "storageConnection";
        private CloudQueue _queue;
        private string connection;

        public Processor(string connection)
        {
            this.connection = connection;
        }

        private CloudQueue queue
        {
            get
            {
                if (_queue == null)
                {
                    var account = CloudStorageAccount.Parse(connection);
                    var client = account.CreateCloudQueueClient();
                    _queue = client.GetQueueReference(QueueName);
                    _queue.CreateIfNotExists();
                }
                return _queue;
            }
        }

        public ResultData GenerateMessages(string message, int count)
        {
            var result = new ResultData();
            try
            {
                for (int lcv = 0; lcv < count; lcv++)
                {
                    queue.AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(new QueueMessage { ID = lcv, Payload = $"{message}-{lcv}" })));
                    result.Code++;
                }
                result.Success = true;
                result.Data.Add("Successfully sent messages");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Code = ex.HResult;
                result.Data.Add(ex.Message);
            }

            return result;
        }

        public ResultData GetMessages()
        {
            var result = new ResultData();
            try
            {
                foreach (var message in queue.GetMessages(32))
                {
                    result.Data.Add(JsonConvert.DeserializeObject<QueueMessage>(message.AsString).Payload);
                    result.Code++;
                    queue.DeleteMessage(message);
                }
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Code = ex.HResult;
                result.Data.Add(ex.Message);
            }

            return result;
        }
    }

    public class QueueMessage
    {
        public int ID { get; set; }
        public string Payload { get; set; }
    }

}
