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
    }
}