using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Part3.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersistController : ControllerBase
    {

        private string redisConnectionStr = "";


        public PersistController(IConfiguration configuration)
        {
            redisConnectionStr = configuration.GetConnectionString("Redis");
        }

        [HttpGet("GetTransaction")]
        public string GetTransaction()
        {
            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var db = redis.GetDatabase();
                var tran = db.CreateTransaction();//MULTI
                tran.AddCondition(Condition.HashNotExists("key", "UniqueID")); //WATCH
                //tran.AddCondition(Condition.StringEqual("name", name));
                bool committed = tran.Execute();    //EXEC
            }
            return "GetTransaction";
        }

        [HttpGet("GetBatch")]
        public string GetBatch()
        {
            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var db = redis.GetDatabase();
                var batch = db.CreateBatch();
                Task t1 = batch.StringSetAsync("name", "bob");
                Task t2 = batch.StringSetAsync("age", 100);
                batch.Execute();
                Task.WaitAll(t1, t2);
            }
            return "GetBatch";
        }

        [HttpGet("GetWait")]
        public string GetWait()
        {
            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var db = redis.GetDatabase();
                var aPending = db.StringSetAsync("a","a");
                var bPending = db.StringSetAsync("b","b");
                var a = db.Wait(aPending);
                var b = db.Wait(bPending);
                //同样可以用aPending.Wait() 或 Task.WaitAll(aPending, bPending)代替
            }
            return "GetWait";
        }
        

    }
}
