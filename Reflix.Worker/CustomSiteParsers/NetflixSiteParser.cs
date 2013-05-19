using HtmlAgilityPack;
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
    class NetflixSiteParser : BaseSiteParser, ICustomSiteParser
    {
        public NetflixSiteParser(string url, DateTime startDate) : base(url, startDate) { }

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
            foreach (var post in posts)
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

        public Title ParseRssItem(Title title)
        {
            return base.ParseNetflixTitle(title);
        }
    }
}
