using LinqToTwitter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Tweetimental.Models;
using Tweetimental.ViewModels;

namespace Tweetimental.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index(string Handle, Int32 Days = 5)
        {
            var vm = new IndexViewModel();

            if ((!string.IsNullOrEmpty(Handle)) && (Days > 0))
            {
                //get tweets
                var statuses = searchUserTweet(Handle, 1, DateTime.UtcNow.Subtract(new TimeSpan(Days, 0, 0, 0)));

                if (statuses.Count > 0)
                {
                    //clean tweets of Twitter specific chars like # or @
                    var stausesAsString = GetCleanedTweetsAsString(statuses);


                    var charLimit = 1400;
                    var averageScore = 0.0;
                    var stausesAs1400String = new List<string>();

                    //check if tweets are blow chunking threshold
                    if (stausesAsString.Length <= charLimit)
                    {
                        averageScore = await GetSentimentScore(stausesAsString);
                    }
                    else
                    {
                        //chunk tweets into blocks of 1400 chars which is the limit for CS text analytics API
                        stausesAs1400String = Enumerable
                            .Range(0, stausesAsString.Length / charLimit)
                            .Select(i => stausesAsString.Substring(i * charLimit, charLimit))
                            .ToList();

                        //get sentiment scores for each block of 1400 chars
                        var scores = new List<double>();
                        foreach (var statusAs1400String in stausesAs1400String)
                        {
                            scores.Add(await GetSentimentScore(statusAs1400String));
                        }

                        //get average for all scores
                        averageScore = scores.Average();
                    }

                    //populate view model
                    vm.Statuses = statuses;
                    vm.Score = averageScore;
                    vm.Message = string.Empty;
                }
                else
                {
                    vm.Statuses = null;
                    vm.Score = 0;
                    vm.Message = "Could not find any Tweets for that handle";
                }

                vm.Handle = Handle;
                vm.Days = Days;
            }

            return View(vm);
        }

        private static List<Status> searchUserTweet(string screenName, int maxPagination, DateTime startDate)
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
            List<Status> searchResults = new List<Status>();
            int maxNumberToFind = 500;
            ulong lastId = 0;
            DateTime lastDateTime = DateTime.UtcNow;

            var tweets = (from tweet in twitterCtx.Status
                          where tweet.Type == StatusType.User &&
                              tweet.ScreenName == screenName &&
                              tweet.Count == maxNumberToFind &&
                              tweet.CreatedAt > startDate
                          select tweet).ToList();

            if (tweets.Count > 0)
            {
                lastId = ulong.Parse(tweets.Last().StatusID.ToString());
                searchResults.AddRange(tweets);
            }

            do
            {
                var id = lastId - 1;
                tweets = (from tweet in twitterCtx.Status
                          where tweet.Type == StatusType.User &&
                              tweet.ScreenName == screenName &&
                              tweet.CreatedAt > startDate &&
                              tweet.MaxID == id
                          select tweet).ToList();

                if (tweets.Count > 0)
                {
                    lastId = tweets.Min(x => x.StatusID);
                    searchResults.AddRange(tweets);
                    lastDateTime = tweets.Min(x => x.CreatedAt);
                }
                else
                {
                    lastDateTime = startDate;
                }

            } while (lastDateTime > startDate);

            return searchResults;
        }

        private static String GetCleanedTweetsAsString(List<Status> statuses)
        {
            var allStausesString = String.Join(" ", statuses.Select(x => x.Text));
            return GetCleanedTweetText(allStausesString);
        }

        private static string GetCleanedTweetText(string tweetText)
        {
            var cleanTweetText = tweetText;
            cleanTweetText = cleanTweetText.Replace("#", "");
            cleanTweetText = cleanTweetText.Replace("@", "");
            cleanTweetText = cleanTweetText.Replace(" RT ", "");
            return cleanTweetText;
        }

        private async Task<double> GetSentimentScore(string inputText)
        {
            var score = 0.0;
            var apiUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";
            var apikey = "c1715e2472b04c1e99ad75ee6a459d64";

            //setup HttpClient with content
            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apikey);

            // Request body
            var sentmentRequestDocument = new SentimentRequestDocument()
            {
                language = "en",
                id = "87",
                text = inputText
            };
            var sentmentRequestDocuments = new List<SentimentRequestDocument>();
            sentmentRequestDocuments.Add(sentmentRequestDocument);
            var requestModel = new SentimentRequest()
            {
                documents = sentmentRequestDocuments
            };
            var requestBodyString = JsonConvert.SerializeObject(requestModel);
            byte[] byteData = Encoding.UTF8.GetBytes(requestBodyString);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(apiUri, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var sentimentResponse = JsonConvert.DeserializeObject<SentimentResponse>(responseData);
                    score = sentimentResponse.documents.FirstOrDefault().score;
                }
                else
                {
                    score = 0.0;
                }
            }

            return score;
        }

    }
}