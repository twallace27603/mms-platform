using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MMSLibrary.Queue;
using MMSLibrary;
using MMSLibrary.Table;
using System.Threading;

namespace PlatformMMS.Controllers
{
    [Route("api/mms")]
    [ApiController]
    public class MMSController : ControllerBase
    {
        private IConfiguration config;
        private QueueProcessor queueProcessor;
        public MMSController(IConfiguration configuration)
        {
            config = configuration;
            queueProcessor = new QueueProcessor(config.GetConnectionString(QueueProcessor.ConnectionName));
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
            var tableProcessor = new TableProcessor(config.GetConnectionString(QueueProcessor.ConnectionName));
            var result = await tableProcessor.GetItems(batchId);
            if(result.Code==0)
            {
                Response.Headers.Add("Cache-Control", "no-cache");
            }
            return result;

        }

        [HttpGet]
        [Route("generateActivity/{id}")]
        public async Task<ResultData> GenerateActivity(int id)
        {
            var result = new ResultData() { Success = false, Code = 0 };
                         var rand = new Random();
           switch(id % 3)
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
                       while((DateTime.Now - start).TotalSeconds < seconds)
                        {
                            double[] data = new double[10000];
                            for(int lcv = 0; lcv < 10000; lcv++)
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
    }
}