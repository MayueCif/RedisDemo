using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Part4.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LogController : ControllerBase
    {

        private string redisConnectionStr = "";


        public LogController(IConfiguration configuration)
        {
            redisConnectionStr = configuration.GetConnectionString("Redis");
        }

        [HttpGet("CreateRecentLog")]
        public string CreateRecentLog(string name,string level,string message)
        {
            var key = $"recent:{name}:{level}";
            message = $"{DateTimeOffset.Now.ToString("yyyy-MM-dd hh:mm:ss")} {message}";
            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var db = redis.GetDatabase();
                var batch = db.CreateBatch();
                Task t1 = batch.ListLeftPushAsync(key, message); //插入日志
                Task t2 = batch.ListTrimAsync(key, 0, 99); //只保留最新的100条记录
                batch.Execute();
                Task.WaitAll(t1, t2);
            }
            return "CreateRecentLog Successed";
        }

        [HttpGet("GetRecentLogs")]
        public string GetRecentLogs()
        {
            return "";
        }

        [HttpGet("CreateCommonLog")]
        public string CreateCommonLog()
        {
            return "";
        }

        [HttpGet("GetCommonLogs")]
        public string GetCommonLogs()
        {
            return "";
        }
    }
}
