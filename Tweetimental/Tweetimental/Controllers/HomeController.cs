using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Tweetimental.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var auth = new MvcAuthorizer
            {
                CredentialStore = new SessionStateCredentialStore
                {
                    ConsumerKey = ConfigurationManager.AppSettings["consumerKey"],
                    ConsumerSecret = ConfigurationManager.AppSettings["consumerSecret"]
                }
            };

            var twitterCtx = new TwitterContext(auth);

            var searchQuery = @"from:martinkearn since:2016-01-07";

            var searchResponse =
                await
                (from search in twitterCtx.Search
                 where search.Type == SearchType.Search &&
                       search.Query == searchQuery &&
                       search.ResultType == ResultType.Recent
                 select search)
                .SingleOrDefaultAsync();

            var allTweets = "";
            foreach (var status in searchResponse.Statuses)
            {
                allTweets += status.Text + " ";
            }

            ViewData["data"] = allTweets;

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}