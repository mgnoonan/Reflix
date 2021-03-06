﻿using HtmlAgilityPack;
using log4net;
using Reflix.Models;
using Reflix.SiteParsing.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Reflix.SiteParsing
{
    public class MoviesDotComSiteParser : BaseSiteParser, ICustomSiteParser
    {
        public MoviesDotComSiteParser(string url, DateTime startDate, string name, ILog log) : base(url, startDate, name, log) { }

        public string Name { get { return base._sourceName; } }

        public List<TitleViewModel> ParseRssList()
        {
            var originalTitles = new List<TitleViewModel>();
            var rssDoc = XDocument.Load(base._sourceUrl);
            //_log.InfoFormat(rssDoc.Element("rss").Element("channel").Element("title").Value);

            // Query the <item>s in the XML RSS data and select each one into a new Post()
            IEnumerable<Post> posts =
                from post in rssDoc.Descendants("item")
                select new Post(post);

            //var newTitleList = new List<TitleViewModel>();

            // Add any RSS entries
            foreach (var post in posts.Where(p => p.Date >= base._sundayWeekOfDate && p.Date <= base._sundayWeekOfDate.AddDays(6)))
            {
                _log.InfoFormat("Parsing '{0}'", post.Title);
                //if (originalTitles.Count(t => t.Title.Name.Equals(post.Title)) == 0)
                //{
                var feedTitle = new MovieTitle
                {
                    Id = "M:" + post.Url.Substring(post.Url.LastIndexOf('/') + 1),
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
                    var newTitle = new TitleViewModel(feedTitle, this.Name, base._sundayWeekOfDate);
                    originalTitles.Add(newTitle);
                }
                else
                {
                    var newTitle = new TitleViewModel(netflixTitle, this.Name, base._sundayWeekOfDate);
                    originalTitles.Add(newTitle);
                }
                //}
            }

            return originalTitles;
        }

        public MovieTitle ParseRssItem(MovieTitle title)
        {
            var document = new HtmlDocument();

            try
            {
                string html = Utils.GetHttpWebResponse(title.Url, null, new System.Net.CookieContainer());
                document.LoadHtml(html);
            }
            catch (Exception ex)
            {
                _log.Error("ParseRssItem", ex);
                return null;
            }

            //try
            //{
            //    // Try to derive the Netflix data, since it is there for some movies

            //    //*[@id="AddQueueWatchIt"]
            //    var nodes = document.DocumentNode.SelectNodes("//*[@id='AddQueueWatchIt']");
            //    var linkNode = nodes.Count == 1 ? nodes[0] : nodes[1];
            //    string href = linkNode.Attributes["onclick"].Value;
            //    string[] attributes = href.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //    foreach (var attribute in attributes)
            //    {
            //        if (!attribute.Contains("addToQueue"))
            //            continue;

            //        int startIndex = attribute.LastIndexOf("/") + 1;
            //        int endIndex = attribute.IndexOf("'", startIndex);
            //        int len = endIndex - startIndex;
            //        string netflixId = attribute.Substring(startIndex, len);
            //        title.Id = netflixId;
            //        title.Url = string.Format("http://dvd.netflix.com/Movie/{0}/{1}", title.Name.Replace(" ", "_"), netflixId);
            //    }

            //    return base.ParseNetflixTitle(title);
            //}
            //catch
            //{
            // Failure parsing the Netflix info, so just pull the info directly from Movies.com

            // Reset the ID
            int startIndex = title.Url.LastIndexOf("/") + 1;
            int endIndex = title.Url.IndexOf("?", startIndex);
            int len = endIndex - startIndex;
            string id = title.Url.Substring(startIndex, len);
            title.Id = "M:" + id;

            // Get the moviespec nodes
            var movieSpecNodes = document.DocumentNode.SelectNodes("//*[@id='movieSpecs']/li");

            // Release date
            //*[@id="movieSpecs"]/li[1]
            var releaseDateNode = ParseMovieSpecByLabel(movieSpecNodes, "Release Date");
            if (releaseDateNode == null)
            {
                title.ReleaseYear = DateTime.Now.Year;
            }
            else
            {
                string releaseDate = releaseDateNode.InnerText.Trim();
                title.ReleaseYear = Convert.ToInt32(releaseDate.Substring(releaseDate.IndexOf(",") + 1));
            }

            // Rating
            //*[@id="movieSpecs"]/li[2]/img
            var ratingNode = ParseMovieSpecByLabel(movieSpecNodes, "Rated"); //document.DocumentNode.SelectSingleNode("//*[@id='movieSpecs']/li[2]/img");
            title.Rating = "N/A";
            if (ratingNode != null)
            {
                var imgNodes = ParseMovieSpecNode(ratingNode.InnerHtml, "//img");
                string rating = imgNodes == null ? "N/A" : imgNodes[0].Attributes["title"].Value.Trim();
                title.Rating = rating.Substring(rating.IndexOf(" ") + 1).ToUpper();
            }

            // Running time
            //*[@id="movieSpecs"]/li[3]
            var runningTimeNode = ParseMovieSpecByLabel(movieSpecNodes, "Runtime"); //document.DocumentNode.SelectSingleNode("//*[@id='movieSpecs']/li[3]");
            string runtime = runningTimeNode == null ? "0" : runningTimeNode.InnerText.Trim();
            title.Runtime = ConvertRunningTime(runtime);

            // Director(s)
            //*[@id="movieSpecs"]/li[5]/a
            var directorLineNode = ParseMovieSpecByLabel(movieSpecNodes, "Director"); //document.DocumentNode.SelectNodes("//*[@id='movieSpecs']/li[5]/a");
            if (directorLineNode != null)
            {
                var directorNodes = ParseMovieSpecNode(directorLineNode.InnerHtml, "//a");
                foreach (var directorNode in directorNodes)
                {
                    string url = directorNode.Attributes["href"].Value.Trim();
                    string parsedID = url.Substring(url.LastIndexOf('/') + 1);
                    title.Directors.Add(new MoviePerson { Id = Convert.ToInt32(parsedID.Substring(1)), Name = directorNode.InnerText.Trim(), Url = url });
                }
            }

            // Cast
            //*[@id="movieSpecs"]/li[6]/a
            var castLineNode = ParseMovieSpecByLabel(movieSpecNodes, "Cast"); //document.DocumentNode.SelectNodes("//*[@id='movieSpecs']/li[6]/a");
            if (castLineNode != null)
            {
                var castNodes = ParseMovieSpecNode(castLineNode.InnerHtml, "//a");
                foreach (var castNode in castNodes)
                {
                    if (castNode.InnerText.Trim() == "Full cast + crew")
                        break;

                    string url = castNode.Attributes["href"].Value;
                    string parsedID = url.Substring(url.LastIndexOf('/') + 1);
                    title.Cast.Add(new MoviePerson { Id = Convert.ToInt32(parsedID.Substring(1)), Name = castNode.InnerText.Trim(), Url = url });
                }
            }

            // Genres
            //*[@id="movieSpecs"]/li[4]
            var genreNode = ParseMovieSpecByLabel(movieSpecNodes, "Genres"); //document.DocumentNode.SelectSingleNode("//*[@id='movieSpecs']/li[4]");
            string genres = genreNode == null ? "0 " : genreNode.InnerText.Trim().Replace("Genres:", string.Empty);
            foreach (var genre in genres.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                title.Genres.Add(genre);
            }

            // Synopsis
            //*[@id="content"]/div[2]/div[1]/p
            var synopsisNode = document.DocumentNode.SelectSingleNode("//*[@id='content']/div[2]/div[1]/p");
            title.Synopsis = synopsisNode.InnerText;

            // BoxArt
            //*[@id="mainContainer"]/div[1]/dl/dt/img
            //var boxartNode = document.DocumentNode.SelectSingleNode("//*[@id='mainContainer']/div[1]/dl/dt/img");
            //title.BoxArt = boxartNode.Attributes["src"].Value;

            return title;
            //}
        }

        private HtmlNode ParseMovieSpecByLabel(HtmlNodeCollection nodes, string labelToSelect)
        {
            foreach (var node in nodes)
            {
                string text = node.InnerText.Trim();
                if (text.StartsWith(labelToSelect))
                {
                    return node;
                }
            }

            return null;
        }

        private HtmlNodeCollection ParseMovieSpecNode(string html, string nodeSelector)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return document.DocumentNode.SelectNodes(nodeSelector);
        }

        private int ConvertRunningTime(string runtime)
        {
            if (runtime == "0")
                return 0;

            string[] values = runtime.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int hours = Convert.ToInt32(values[1]);
            int minutes = Convert.ToInt32(values[3]);

            return (hours * 60) + minutes;
        }
    }
}
