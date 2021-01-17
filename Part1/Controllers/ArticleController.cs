using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Part1.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Part1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ArticleController : ControllerBase
    {

        private static readonly Article[] Articles = new[]
        {
            new Article{ ID=1, Title="文章1",Link="",UnixTime=DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeMilliseconds(),Poster=1,Votes=0 },
            new Article{ ID=2, Title="文章2",Link="",UnixTime=DateTimeOffset.UtcNow.AddDays(4).ToUnixTimeMilliseconds(),Poster=2,Votes=0 },
            new Article{ ID=3, Title="文章3",Link="",UnixTime=DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeMilliseconds(),Poster=3,Votes=0 },
            new Article{ ID=4, Title="文章4",Link="",UnixTime=DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeMilliseconds(),Poster=4,Votes=0 },
            new Article{ ID=5, Title="文章5",Link="",UnixTime=DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeMilliseconds(),Poster=5,Votes=0 }
        };

        private readonly ILogger<ArticleController> _logger;

        private readonly string redisConnectionStr = "localhost,abortConnect=false";

        public ArticleController(ILogger<ArticleController> logger)
        {
            _logger = logger;
        }

        [HttpGet("InitArticles")]
        public string InitArticles()
        {
            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var db = redis.GetDatabase();
                foreach (var article in Articles)
                {
                    var articleId = $"article:{article.ID}";
                    var hashEntiies = new [] {
                        new HashEntry(nameof(article.Title),article.Title),
                        new HashEntry(nameof(article.Link),article.Link),
                        new HashEntry(nameof(article.Poster),article.Poster),
                        new HashEntry(nameof(article.Votes),article.Votes),
                        new HashEntry(nameof(article.UnixTime),article.UnixTime),
                    };
                    db.HashSetAsync(articleId, hashEntiies);

                    db.SortedSetAddAsync("score:",articleId, 0);
                    db.SortedSetAddAsync("time:",articleId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    Thread.Sleep(1000);
                }
            }
            return "文章初始化完成";
        }

        [HttpGet("ArticleVote")]
        public string ArticleVote(int userId,int articleId) {

            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var db = redis.GetDatabase();
                if (db.SetAdd($"voted:{articleId}",$"user:{userId}"))
                {
                    //增加投票票数和评分
                    db.HashIncrementAsync($"article:{articleId}", "Votes", 1);
                    db.SortedSetIncrementAsync("score:", $"article:{articleId}", 1);
                }
                else
                {
                    return "不能重复投票";
                }
            }
            return "投票成功";
        }

        [HttpGet("GetArticles")]
        public IEnumerable<Article> GetArticles(int page,int size) {

            var articles = new List<Article>();
            using (var redis = ConnectionMultiplexer.Connect(redisConnectionStr))
            {
                var start = (page - 1) * size;
                var end = start + size - 1;

                var db = redis.GetDatabase();
                var ids = db.SortedSetRangeByRank("score:", start, end,order:Order.Descending);
                foreach (var id in ids)
                {
                    var articleHashEntities = db.HashGetAll(id.ToString());
                    //这里除了用反射没有找到好的解决办法
                    var article = new Article {
                        ID = int.Parse(id.ToString().Split(':')[1]),
                        Title= articleHashEntities.FirstOrDefault(a=>a.Name=="Title").Value,
                        Link= articleHashEntities.FirstOrDefault(a => a.Name == "Link").Value,
                        Poster= int.Parse(articleHashEntities.FirstOrDefault(a => a.Name == "Poster").Value),
                        UnixTime= long.Parse(articleHashEntities.FirstOrDefault(a => a.Name == "UnixTime").Value),
                        Votes= int.Parse(articleHashEntities.FirstOrDefault(a => a.Name == "Votes").Value)
                    };
                    articles.Add(article);
                }
                
            }
            return articles;
        }

    }
}
