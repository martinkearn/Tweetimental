using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tweetimental.ViewModels
{
    public class IndexViewModel
    {
        public List<Status> Statuses { get; set; }
        public double Score { get; set; }
        public string Message { get; set; }
        public string Handle { get; set; }

        public int Days { get; set; }
    }
}
