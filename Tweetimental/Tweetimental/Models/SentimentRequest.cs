using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tweetimental.Models
{
    public class SentimentRequest
    {
        public List<SentimentRequestDocument> documents { get; set; }
    }

    public class SentimentRequestDocument
    {
        public string language { get; set; }
        public string id { get; set; }
        public string text { get; set; }
    }
}
