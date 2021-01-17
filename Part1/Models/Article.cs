using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Part1.Models
{
    public class Article
    {

        public int ID { get; set; }

        public string Title { get; set; }

        public string Link { get; set; }

        //public DateTimeOffset Time { get; set; }

        public long UnixTime { get; set; }

        public int Poster { get; set; }

        public int Votes { get; set; }
    }
}
