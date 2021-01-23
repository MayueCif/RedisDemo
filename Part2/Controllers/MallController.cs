using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Part2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MallController : ControllerBase
    {
        private string redisConnectionStr = "";


        public MallController(IConfiguration configuration)
        {
            redisConnectionStr = configuration.GetConnectionString("Redis");
        }

        [HttpGet("UpdateCart")]
        public string UpdateCart(string token,string item,int count)
        {
            var key = $"cart:{token}";
            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var db = redis.GetDatabase();
                //更新购物车数据
                if (count <= 0)
                {
                    db.HashDeleteAsync(key, item);
                }
                else
                {
                    db.HashSetAsync(key, item,count);
                }
            }
            
            return "更新购物车成功";
        }


        [HttpGet("GetDateTime")]
        public string GetDateTime()
        {
            var dateTime = "";
            var key = "data:datetime";
            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var db = redis.GetDatabase();
                if (db.KeyExists(key))
                {
                    dateTime = db.StringGet(key);
                }
                else
                {
                    dateTime = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    db.StringSet(key, dateTime,expiry:TimeSpan.FromSeconds(5));
                }
            }

            return dateTime;
        }

    }
}
