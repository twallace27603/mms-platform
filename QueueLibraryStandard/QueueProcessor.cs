using System;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace MMSLibrary.Queue
{
    public class QueueProcessor
    {
        public const string QueueName = "process";
        public const string TableName = "data";
        public const string ConnectionName = "storageConnection";
        private CloudQueue _queue;
        private string connection;

        public QueueProcessor(string connection)
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
                    _queue.CreateIfNotExistsAsync().GetAwaiter();
                }
                return _queue;
            }
        }

        public async Task<ResultData> GenerateMessages(List<DatagramItem> data)
        {
            var result = new ResultData();
            try
            {
                foreach (var message in data)
                {
                    await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(message)));
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

        public async Task<ResultData> GetMessages()
        {
            var result = new ResultData();
            try
            {
                foreach (var message in await queue.GetMessagesAsync(32))
                {
                    result.Data.Add(JsonConvert.DeserializeObject<DatagramItem>(message.AsString).Payload);
                    result.Code++;
                    await queue.DeleteMessageAsync(message);
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
