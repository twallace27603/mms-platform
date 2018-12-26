using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MMSLibrary.Table
{
    public class TableProcessor
    {
        private CloudTable _table;
        private string connectionString;
        public const string TableName = "processed";

        public TableProcessor(string connection)
        {
            connectionString = connection;
        }

        private CloudTable table
        {
            get
            {
                if(_table==null)
                {
                    var account = CloudStorageAccount.Parse(connectionString);
                    var client = account.CreateCloudTableClient();
                    _table = client.GetTableReference(TableName);
                    _table.CreateIfNotExistsAsync().GetAwaiter();
                }
                return _table;
            }
        }
        public async Task AddItem(DatagramItem item)
        {
            var entity = new DatagramItemEntity(item);
            var insert = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(insert);
            Trace.Write($"Added table row: {item.BatchId}:{item.ID}");
        }

        public async Task<ResultData> GetItems(string batchID)
        {
            var result = new ResultData();
            var query = new TableQuery<DatagramItemEntity>();
            query.FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, batchID);
            try
            {
                var rows = await table.ExecuteQuerySegmentedAsync<DatagramItemEntity>(query, null);
                foreach (var row in rows)
                {
                    result.Data.Add(row.ToString());
                    result.Code++;
                }
                result.Success = true;
            } catch (Exception ex)
            {
                result.Success = false;
                result.Code = ex.HResult;
                result.Data.Add(ex.Message);
            }

            return result;

        }
       
    }

    public class DatagramItemEntity:TableEntity
    {
        public DatagramItemEntity() { }
        public DatagramItemEntity(DatagramItem item)
        {
            this.PartitionKey = item.BatchId;
            this.RowKey = item.ID.ToString();
            this.Payload = item.Payload;
        }
        public string Payload { get; set; }

        public DatagramItem AsDatagramItem()
        {

            return new DatagramItem
            {
                BatchId = PartitionKey,
                ID = int.Parse(RowKey),
                Payload = Payload
            };
        }
        public override string ToString()
        {
            return $"{RowKey}: {Payload}";
        }

    }
}
