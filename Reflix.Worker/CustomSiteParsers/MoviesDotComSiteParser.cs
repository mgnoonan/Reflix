﻿using HtmlAgilityPack;
using Reflix.Models;
using Reflix.Worker.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Reflix.Worker.CustomSiteParsers
{
    class MoviesDotComSiteParser : BaseSiteParser, ICustomSiteParser
    {
        public MoviesDotComSiteParser(string url, DateTime startDate, string name) : base(url, startDate, name) { }

        public string Name { get { return base._name; } }

        public List<TitleViewModel> ParseRssList()
        {
            var originalTitles = new List<TitleViewModel>();
            var rssDoc = XDocument.Load(base._url);
            //Console.WriteLine(rssDoc.Element("rss").Element("channel").Element("title").Value);

            // Query the <item>s in the XML RSS data and select each one into a new Post()
            IEnumerable<Post> posts =
                from post in rssDoc.Descendants("item")
                select new Post(post);

            var newTitleList = new List<TitleViewModel>();

            // Add any RSS entries
            foreach (var post in posts.Where(p => p.Date >= base._startDate))
            {
                if (originalTitles.Count(t => t.Title.Name.Equals(post.Title)) == 0)
                {
                    var feedTitle = new MovieTitle
                    {
                        Id = post.Guid.Substring(post.Guid.LastIndexOf('/') + 1),
                        Name = post.Title,
                        Url = post.Url,
                        Synopsis = post.Description,
                        Cast = new List<MoviePerson>(),
                        Directors = new List<MoviePerson>(),
                        Genres = new List<string>(),
                        BoxArt = post.ImageUrl,
                        ReleaseYear = DateTime.Now.Year,
                        Rating = "N/A",
                        Runtime = 0
                    };

                    MovieTitle netflixTitle = null;
                    netflixTitle = ParseRssItem(feedTitle);

                    if (netflixTitle == null)
                    {
                        var newTitle = new TitleViewModel(feedTitle, true, base._startDate);
                        originalTitles.Add(newTitle);
                    }
                    else
                    {
                        var newTitle = new TitleViewModel(netflixTitle, true, base._startDate);
                        originalTitles.Add(newTitle);
                    }
                }
            }

            return originalTitles;
        }

        public MovieTitle ParseRssItem(MovieTitle title)
        {
            string html = Utils.GetHttpWebResponse(title.Url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            //*[@id="AddQueueWatchIt"]
            var nodes = document.DocumentNode.SelectNodes("//*[@id='AddQueueWatchIt']");
            string href = nodes[1].Attributes["onclick"].Value;
            string[] attributes = href.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var attribute in attributes)
            {
                if (!attribute.Contains("addToQueue"))
                    continue;

                int startIndex = attribute.LastIndexOf("/") + 1;
                int endIndex = attribute.IndexOf("'", startIndex);
                int len = endIndex - startIndex;
                string netflixId = attribute.Substring(startIndex, len);
                title.Id = netflixId;
                title.Url = string.Format("http://dvd.netflix.com/Movie/{0}/{1}", title.Name.Replace(" ", "_"), netflixId);
            }

            return base.ParseNetflixTitle(title);
        }
    }
}
