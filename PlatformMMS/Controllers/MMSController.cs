using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MMSLibrary;
using MMSLibrary.Queue;
using MMSLibrary.Table;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformMMS.Controllers
{
    [Route("api/mms")]
    [ApiController]
    public class MMSController : ControllerBase
    {
        private IConfiguration _configuration;
        private IMemoryCache _cache;
        private QueueProcessor queueProcessor;
        public MMSController(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
            queueProcessor = new QueueProcessor(_configuration.GetConnectionString(QueueProcessor.ConnectionName));
        }

        [HttpPost]
        [Route("generateQueueMessages")]
        public async Task<ResultData> GenerateQueueMessages(Datagram data)
        {
            return await queueProcessor.GenerateMessages(data.Items);
        }

        [HttpGet]
        [Route("getQueueMessages")]
        public async Task<ResultData> GetMessages()
        {
            return await queueProcessor.GetMessages();
        }

        [HttpGet]
        [Route("getTableRows")]
        public async Task<ResultData> GetRows(string batchId)
        {
            var tableProcessor = new TableProcessor(_configuration.GetConnectionString(QueueProcessor.ConnectionName));
            var result = await tableProcessor.GetItems(batchId);
            if (result.Code == 0)
            {
                Response.Headers.Add("Cache-Control", "no-cache");
            }
            return result;

        }

        [HttpGet]
        [Route("generateActivity/{id:int=-1}")]
        public async Task<ResultData> GenerateActivity(int id)
        {
            var result = new ResultData() { Success = false, Code = 0 };
            var rand = new Random();
            if (id == -1) { id = rand.Next(3); }
            switch (id % 3)
            {
                case 1: //Wait a bit and then return something
                    result = await Task.Run<ResultData>(() =>
                    {
                        var seconds = 1 + rand.Next(4);
                        Thread.Sleep(seconds * 1000);
                        var delayResult = new ResultData() { Code = 1, Success = true };
                        delayResult.Data.Add($"Run # {id}: Waited for {seconds} seconds at {DateTime.Now.ToLongTimeString()}.");
                        return delayResult;
                    });
                    break;
                case 2: //Generate some processor load
                    result = await Task.Run<ResultData>(() =>
                    {
                        var loadResult = new ResultData { Code = 2, Success = true };
                        var start = DateTime.Now;
                        int seconds = 3 + rand.Next(3);
                        while ((DateTime.Now - start).TotalSeconds < seconds)
                        {
                            double[] data = new double[10000];
                            for (int lcv = 0; lcv < 10000; lcv++)
                            {
                                data[lcv] = rand.NextDouble() * double.MaxValue;
                            }
                            Array.Sort(data);
                        }
                        loadResult.Data.Add($"Run {id}: Processed data for {(DateTime.Now - start).TotalSeconds} seconds at {DateTime.Now.ToLongTimeString()}");
                        return loadResult;
                    });

                    break;
                default:
                    switch (rand.Next(3))
                    {
                        case 0:
                            throw new IndexOutOfRangeException($"Run {id} generated an error.");
                        case 1:
                            throw new InvalidOperationException($"Run {id} generated an error.");
                        default:
                            throw new Exception($"Run {id} generated an error.");
                    }
            }
            return result;
        }

        [HttpPost]
        [Route("notify")]
        public void Notify([FromBody]object payload)
        {
            List<string> notices;
            if (!_cache.TryGetValue("notices", out notices))
            {
                notices = new List<string>();
            }
            notices.Add(payload.ToString());
            _cache.Set("notices", notices);
        }

        [HttpGet]
        [Route("getNotices")]
        public ResultData GetNotices()
        {
            ResultData result = new ResultData();
            try
            {
                List<string> notices;
                
                if(!_cache.TryGetValue<List<string>>("notices", out notices))
                {
                    notices = new List<string>();
                }
               
                result = new ResultData { Success = true, Code = notices.Count, Data = notices };
            } catch(Exception ex)
            {
                result.Success = false;
                result.Code = ex.HResult;
                result.Data.Add(ex.Message);
            }
            return result;
        }

        [HttpGet]
        [Route("testKeyVault")]
        public async Task<ResultData> TestKeyVault()
        {
            ResultData result = new ResultData { Code = 0, Success = false };
            try
            {
                int retries = 0;
                bool retry = false;
                string connection = "";
                string errorMessage = "";
                string secretUri = _configuration.GetValue(typeof(string), "secretUri").ToString(); ;
                try
                {
                    /* The below 4 lines of code shows you how to use AppAuthentication library to fetch secrets from your Key Vault*/
                    AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                    KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                    var secret = await keyVaultClient.GetSecretAsync(secretUri)
                            .ConfigureAwait(false);
                    connection = secret.Value;

                    /* The below do while logic is to handle throttling errors thrown by Azure Key Vault. It shows how to do exponential backoff which is the recommended client side throttling*/
                    do
                    {

                        secret = await keyVaultClient.GetSecretAsync(secretUri)
                            .ConfigureAwait(false);
                        retry = false;
                    }
                    while (retry && (retries++ < 10));
                    connection = secret.Value;
                }
                /// <exception cref="KeyVaultErrorException">
                /// Thrown when the operation returned an invalid status code
                /// </exception>
                catch (KeyVaultErrorException keyVaultException)
                {
                    connection = "";
                    errorMessage = keyVaultException.ToString();
                    if ((int)keyVaultException.Response.StatusCode == 429)
                        retry = true;
                }
                if (!string.IsNullOrEmpty(connection))
                {
                    var processor = new QueueProcessor(connection);
                    var items = new List<DatagramItem>();
                    items.Add(new DatagramItem { BatchId = DateTime.Now.ToString(), ID = 1, Payload = "Key vault test" });
                    result = await processor.GenerateMessages(items);
                    if (result.Success)
                    {
                        result = await processor.GetMessages();
                    }

                }
                else
                {
                    result.Data.Add(errorMessage);
                }
            } catch(Exception ex)
            {
                result.Code = ex.HResult;
                result.Data.Add(ex.ToString());
            }
            return result;

        }

    }
}