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
                Console.WriteLine("Checking release date '{0}'", post.Title);
                var releaseDate = ParseReleaseDate(post);
                if (releaseDate.Date <= base._startDate.Date.AddDays(-7))
                    continue;

                Console.WriteLine("Parsing '{0}'", post.Title);
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
            }

            return originalTitles;
        }

        private DateTime ParseReleaseDate(Post post)
        {
            string html = Utils.GetHttpWebResponse(post.Url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // Get the link to more release info
            //*[@id="mainContainer"]/div[1]/div/dl[4]/dd/a
            //*[@id="mainContainer"]/div[1]/div/dl[3]/dd/a
            var linkNode = document.DocumentNode.SelectSingleNode("//*[@id='mainContainer']/div[1]/div/dl[4]/dd/a");
            if (linkNode == null)
            {
                linkNode = document.DocumentNode.SelectSingleNode("//*[@id='mainContainer']/div[1]/div/dl[3]/dd/a");
            }
            string href = "http://www.blockbuster.com" + linkNode.Attributes["href"].Value.Trim();

            html = Utils.GetHttpWebResponse(href, null, new System.Net.CookieContainer());
            document = new HtmlDocument();
            document.LoadHtml(html);

            // Get the release date
            // /html/body/div[1]/div[6]/div[1]/div[1]/div[2]/div[2]
            // /html/body/div[1]/div[6]/div[1]/div[1]/div[5]/div[2]
            var releaseNode = document.DocumentNode.SelectSingleNode("/html/body/div[1]/div[6]/div[1]/div[1]/div[2]/div[2]");
            string para = releaseNode.InnerText.Trim();
            if (para.Contains("Format: DVD"))
            {
                int startIndex = para.IndexOf("Release Date: ") + 14;
                int len = 10;
                string releaseDate = para.Substring(startIndex, len);
                return DateTime.Parse(releaseDate);
            }
            else
            {
                releaseNode = document.DocumentNode.SelectSingleNode("/html/body/div[1]/div[6]/div[1]/div[1]/div[5]/div[2]");
                para = releaseNode.InnerText.Trim();
                if (para.Contains("Format: DVD"))
                {
                    int startIndex = para.IndexOf("Release Date: ") + 14;
                    int len = 10;
                    string releaseDate = para.Substring(startIndex, len);
                    return DateTime.Parse(releaseDate);
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
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
                string url = "http://www.blockbuster.com" + directorNode.Attributes["href"].Value;
                string parsedID = url.Substring(url.LastIndexOf('/') + 1);
                title.Directors.Add(new MoviePerson { Id = Convert.ToInt32(parsedID), Name = directorNode.InnerText.Trim(), Url = url });
            }

            // Cast
            //*[@id="tabPanel1"]/p/a[1]
            var castNodes = document.DocumentNode.SelectNodes("//*[@id='tabPanel1']/p/a");
            foreach (var castNode in castNodes)
            {
                string url = "http://www.blockbuster.com" + castNode.Attributes["href"].Value;
                string parsedID = url.Substring(url.LastIndexOf('/') + 1);
                title.Cast.Add(new MoviePerson { Id = Convert.ToInt32(parsedID), Name = castNode.InnerText.Trim(), Url = url });
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
