using HtmlAgilityPack;
using NCI.Utility;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Reflix.Models;
using Reflix.Netflix;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace Reflix.Controllers
{
    public class HomeController : BaseRavenController
    {
        //private static bool _serviceDown = false;

        public ActionResult Index(DateTime? startDate)
        {
            try
            {
                DateTime calculatedStartDate;
                if (!startDate.HasValue)
                    calculatedStartDate = CalculateStartDate();
                else
                    calculatedStartDate = startDate.Value.Date;

                var calculatedEndDate = calculatedStartDate.AddDays(6);

                //var modelList = GetODataTitles(newStartDate, endDate);
                //AddRssTitles(modelList, newStartDate);
                var modelList = GetRssTitlesFromEmbeddedStore(calculatedStartDate, calculatedEndDate);

                ViewBag.Message = string.Format("Week of {0}", calculatedStartDate.ToString("d-MMM-yyyy"));
                ViewBag.StartDate = calculatedStartDate;
                ViewBag.EndDate = calculatedEndDate;

                return View("IndexMBS", modelList.OrderBy(t => t.Title.Name).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
                throw;
            }
        }

        private DateTime CalculateStartDate()
        {
            DateTime testDate = DateTime.Now.Date;

            //if (testDate.Day == (int)DayOfWeek.Sunday)
            //    return testDate;

            return testDate.AddDays(-(int)testDate.DayOfWeek);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Data sources:";

            return View();
        }

        private void AddRssTitles(List<TitleViewModel> originalTitles, DateTime newStartDate)
        {
            //if (GetRssTitlesFromEmbeddedStore(originalTitles, newStartDate))
            //{
            //    return;
            //}

            //if (newStartDate != CalculateStartDate())
            //{
            //    return;
            //}

            GetRssTitles(originalTitles, "http://rss.netflix.com/NewReleasesRSS", newStartDate);
            //GetRssTitles(originalTitles, "http://www.movies.com/rss-feeds/new-on-dvd-rss", newStartDate);

            //return newTitleList;
        }

        private void GetRssTitles(List<TitleViewModel> originalTitles, string url, DateTime newStartDate)
        {

            XDocument rssDoc = XDocument.Load(url);
            //Console.WriteLine(rssDoc.Element("rss").Element("channel").Element("title").Value);

            // Query the <item>s in the XML RSS data and select each one into a new Post()
            IEnumerable<Post> posts =
                from post in rssDoc.Descendants("item")
                select new Post(post);

            var newTitleList = new List<TitleViewModel>();

            // Add any RSS entries
            foreach (var post in posts.Where(p => p.Date >= newStartDate))
            {
                if (originalTitles.Count(t => t.Title.Name.Equals(post.Title)) == 0)
                {
                    var feedTitle = new Title
                    {
                        Id = post.Guid.Substring(post.Guid.LastIndexOf('/') + 1),
                        Name = post.Title,
                        Url = post.Url,
                        Synopsis = post.Description,
                        Cast = new Collection<Person>(),
                        Directors = new Collection<Person>(),
                        Genres = new Collection<Genre>(),
                        BoxArt = new BoxArt { LargeUrl = post.ImageUrl },
                        ReleaseYear = DateTime.Now.Year,
                        Rating = "N/A",
                        Runtime = 0
                    };

                    Title netflixTitle = null;
                    switch(url)
                    {
                        case "http://rss.netflix.com/NewReleasesRSS":
                            netflixTitle = GetNetflixData(feedTitle);
                            break;
                        case "http://www.movies.com/rss-feeds/new-on-dvd-rss":
                            netflixTitle = GetMoviesDotComData(feedTitle);
                            break;
                    }
                    
                    if (netflixTitle == null)
                    {
                        var newTitle = new TitleViewModel(feedTitle, true, newStartDate);
                        originalTitles.Add(newTitle);
                        this.RavenSession.Store(newTitle);
                    }
                    else
                    {
                        var newTitle = new TitleViewModel(netflixTitle, true, newStartDate);
                        originalTitles.Add(newTitle);
                        this.RavenSession.Store(newTitle);
                    }
                }
            }
        }

        private Title GetMoviesDotComData(Title title)
        {
            string html = Utils.GetHttpWebResponse(title.Url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            //*[@id="AddQueueWatchIt"]
            var node = document.DocumentNode.SelectSingleNode("//*[@id='AddQueueWatchIt']");
            string href = node.Attributes["onclick"].Value;
            string[] attributes = href.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach(var attribute in attributes)
            {
                if (attribute.Contains("addToQueue"))
                {
                    int startIndex = href.LastIndexOf("/");
                    int endIndex = href.LastIndexOf(")", startIndex);
                    int len = endIndex - startIndex;
                    title.Url = string.Format("http://dvd.netflix.com/Movie/{0}/{1}", title.Name.Replace(" ", "_"), attribute.Substring(startIndex, len));
                }
            }

            return GetNetflixData(title);
        }

        private static Title GetNetflixData(Title title)
        {
            //if (_serviceDown)
            //    return null;

            //try
            //{
            //    var ctx = new Netflix.NetflixCatalog(new Uri("http://odata.netflix.com/Catalog/"));
            //    var query = from m in ctx.Titles.Expand("Dvd").Expand("Genres").Expand("Cast").Expand("Directors")
            //                where m.Name == post.Title //&& m.Dvd.AvailableFrom.Value.Year >= DateTime.Now.AddDays(-7).Year
            //                orderby m.Name
            //                select m;

            //    return query.FirstOrDefault();
            //}
            //catch
            //{
            //    return null;
            //}

            string html = Utils.GetHttpWebResponse(title.Url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            //*[@id="nmmdp"]/table/tr/td/div/div[1]/span
            var nodes = document.DocumentNode.SelectNodes("//*[@id='nmmdp']/table/tr/td/div/div[1]/span");
            title.ReleaseYear = Convert.ToInt32(nodes[0].InnerText);
            title.Rating = nodes[1].InnerText;
            string runtime = nodes[2].InnerText;
            title.Runtime = Convert.ToInt32(runtime.Substring(0, runtime.IndexOf(' ')).Trim());

            try
            {
                //*[@id="support"]/div[1]/a
                nodes = document.DocumentNode.SelectNodes("//*[@id='support']/div[1]/a");
                foreach (var node in nodes)
                {
                    string parsedID = node.Attributes["href"].Value;
                    parsedID = parsedID.Substring(parsedID.LastIndexOf('/') + 1);
                    title.Cast.Add(new Person { Id = Convert.ToInt32(parsedID), Name = node.InnerText.Trim() });
                }
            }
            catch { }

            try
            {
                //*[@id="support"]/div[2]/a
                nodes = document.DocumentNode.SelectNodes("//*[@id='support']/div[2]/a");
                foreach (var node in nodes)
                {
                    string parsedID = node.Attributes["href"].Value;
                    parsedID = parsedID.Substring(parsedID.LastIndexOf('/') + 1);
                    title.Directors.Add(new Person { Id = Convert.ToInt32(parsedID), Name = node.InnerText.Trim() });
                }
            }
            catch { }

            try
            {
                //*[@id="support"]/div[3]/a
                nodes = document.DocumentNode.SelectNodes("//*[@id='support']/div[3]");
                foreach (var node in nodes)
                {
                    title.Genres.Add(new Genre { Name = node.InnerText.Replace("Genre:", string.Empty).Trim() });
                }
            }
            catch { }

            return title;
        }

        private List<TitleViewModel> GetRssTitlesFromEmbeddedStore(DateTime newStartDate, DateTime newEndDate)
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        where title.RssDate >= newStartDate && title.RssDate <= newEndDate
                        select title;

            return query.ToList();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
