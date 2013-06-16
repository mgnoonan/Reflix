using HtmlAgilityPack;
using Reflix.Models;
using Reflix.Worker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Reflix.Worker.CustomSiteParsers
{
    class BlockbusterSiteParser : BaseSiteParser, ICustomSiteParser
    {
        public BlockbusterSiteParser(string url, DateTime startDate, string name) : base(url, startDate, name) { }

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

            //var newTitleList = new List<TitleViewModel>();

            // Add any RSS entries
            foreach (var post in posts) //.Where(p => p.Date >= base._startDate && p.Date <= base._startDate.AddDays(6)))
            {
                Console.WriteLine("Parsing '{0}'", post.Title);
                //if (originalTitles.Count(t => t.Title.Name.Equals(post.Title)) == 0)
                //{
                var feedTitle = new MovieTitle
                {
                    Id = "B:" + post.Url.Substring(post.Url.LastIndexOf('/') + 1),
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
                    var newTitle = new TitleViewModel(feedTitle, this.Name, base._startDate);
                    originalTitles.Add(newTitle);
                }
                else
                {
                    var newTitle = new TitleViewModel(netflixTitle, this.Name, base._startDate);
                    originalTitles.Add(newTitle);
                }
                //}
            }

            return originalTitles;
        }

        public MovieTitle ParseRssItem(MovieTitle title)
        {
            string html = Utils.GetHttpWebResponse(title.Url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // Release Year
            // /html/body/div[1]/div[6]/div[1]/div[1]/h1
            var titleHeaderNode = document.DocumentNode.SelectSingleNode("/html/body/div[1]/div[6]/div[1]/div[1]/h1");
            string titleHeader = titleHeaderNode == null ? string.Format("{0} ({1})", title.Name, title.ReleaseYear) : titleHeaderNode.InnerText;
            if (titleHeader.IndexOf("(") >= 0)
            {
                int startIndex = titleHeader.IndexOf("(") + 1;
                int endIndex = titleHeader.LastIndexOf(")");
                int len = endIndex - startIndex;
                string releaseYear = titleHeader.Substring(startIndex, len);
                title.ReleaseYear = Convert.ToInt32(releaseYear);
            }

            // Rating
            //*[@id="tabPanel1"]/dl[5]/dd
            var ratingNode = document.DocumentNode.SelectSingleNode("//*[@id='tabPanel1']/dl[5]/dd");
            string rating = ratingNode == null ? "N/A" : ratingNode.InnerText;
            title.Rating = rating.Substring(0, rating.IndexOf("("));

            // Running time
            //*[@id="tabPanel1"]/dl[1]/dd
            var runningTimeNode = document.DocumentNode.SelectSingleNode("//*[@id='tabPanel1']/dl[1]/dd");
            string runtime = runningTimeNode == null ? "0 " : runningTimeNode.InnerText;
            title.Runtime = Convert.ToInt32(runtime.Substring(0, runtime.IndexOf('&')).Trim());

            // Director(s)
            //*[@id="tabPanel1"]/dl[2]/dd/a
            var directorNodes = document.DocumentNode.SelectNodes("//*[@id='tabPanel1']/dl[2]/dd");
            foreach (var directorNodeDD in directorNodes)
            {
                var directorNode = directorNodeDD.SelectSingleNode(directorNodeDD.XPath + "/a");
                string parsedID = directorNode.Attributes["href"].Value;
                parsedID = parsedID.Substring(parsedID.LastIndexOf('/') + 1);
                title.Directors.Add(new MoviePerson { Id = Convert.ToInt32(parsedID), Name = directorNode.InnerText.Trim() });
            }

            // Synopsis
            //*[@id="tabPanel1"]/p
            var synopsisNode = document.DocumentNode.SelectSingleNode("//*[@id='tabPanel1']/p");
            title.Synopsis = synopsisNode.InnerText;

            // BoxArt
            //*[@id="mainContainer"]/div[1]/dl/dt/img
            var boxartNode = document.DocumentNode.SelectSingleNode("//*[@id='mainContainer']/div[1]/dl/dt/img");
            title.BoxArt = boxartNode.Attributes["src"].Value;

            return title;
        }
    }
}
