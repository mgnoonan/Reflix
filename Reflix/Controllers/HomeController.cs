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
        public ActionResult Index(DateTime? startDate)
        {
            DateTime newStartDate;
            if (!startDate.HasValue)
                newStartDate = CalculateStartDate();
            else
                newStartDate = startDate.Value.Date;

            var endDate = newStartDate.AddDays(6);

            var ctx = new Netflix.NetflixCatalog(new Uri("http://odata.netflix.com/Catalog/"));
            var query = from m in ctx.Titles.Expand("Dvd").Expand("Genres").Expand("Cast").Expand("Directors")
                        where m.Dvd.AvailableFrom >= newStartDate && m.Dvd.AvailableFrom <= endDate
                        orderby m.Name
                        select m;

            var list = query.ToList();
            var modelList = list.Select(l => new TitleViewModel
                        {
                            Title = l,
                            IsRss = false
                        }).ToList();

            //if (!startDate.HasValue)
            //{
                AddRssTitles(modelList, newStartDate);
            //}

            ViewBag.Message = string.Format("DVD releases for the week of {0} through {1}.", newStartDate.ToString("MMMM d, yyyy"), endDate.ToString("MMMM d, yyyy")); ;
            ViewBag.StartDate = newStartDate;
            ViewBag.EndDate = endDate;

            return View("Index", modelList.OrderBy(t => t.Title.Name).ToList());
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
            ViewBag.Message = "Your app description page.";

            return View();
        }

        private void AddRssTitles(List<TitleViewModel> originalTitles, DateTime newStartDate)
        {
            if (AddRssTitlesFromEmbeddedStore(originalTitles, newStartDate))
            {
                return;
            }

            if (newStartDate != CalculateStartDate())
            {
                return;
            }

            XDocument rssDoc = XDocument.Load("http://rss.netflix.com/NewReleasesRSS");
            //Console.WriteLine(rssDoc.Element("rss").Element("channel").Element("title").Value);

            // Query the <item>s in the XML RSS data and select each one into a new Post()
            IEnumerable<Post> posts =
                from post in rssDoc.Descendants("item")
                select new Post(post);

            var ctx = new Netflix.NetflixCatalog(new Uri("http://odata.netflix.com/Catalog/"));
            var newTitleList = new List<TitleViewModel>();

            // Add any RSS entries
            foreach (var post in posts)
            {
                if (originalTitles.Count(t => t.Title.Name.Equals(post.Title)) == 0)
                {
                    var query = from m in ctx.Titles.Expand("Dvd").Expand("Genres").Expand("Cast").Expand("Directors")
                                where m.Name == post.Title //&& m.Dvd.AvailableFrom.Value.Year >= DateTime.Now.AddDays(-7).Year
                                orderby m.Name
                                select m;

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

                    var netflixTitle = query.FirstOrDefault();
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

            //return newTitleList;
        }

        private bool AddRssTitlesFromEmbeddedStore(List<TitleViewModel> originalTitles, DateTime newStartDate)
        {
            bool result = false;

            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        where title.RssDate == newStartDate
                        select title;

            if (query.Any())
            {
                originalTitles.AddRange(query);
                result = true;
            }

            return result;
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
