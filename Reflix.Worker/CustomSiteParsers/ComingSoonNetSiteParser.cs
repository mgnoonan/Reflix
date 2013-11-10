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
    class ComingSoonNetSiteParser : BaseSiteParser, ICustomSiteParser
    {
        public ComingSoonNetSiteParser(string url, DateTime startDate, string name) : base(url, startDate, name) { }

        public string Name { get { return base._sourceName; } }

        public List<Models.TitleViewModel> ParseRssList()
        {
            var originalTitles = new List<TitleViewModel>();

            // Determine correct start and end dates for the web page
            // For startDate, we want the Tuesday following the sundayWeekOf date
            // For endDate, we want exactly 1 week later (which may end up in the following month)
            DateTime startDate = this._sundayWeekOfDate.AddDays(2);
            DateTime endDate = startDate.AddDays(7);

            // Determine the correct URL for the given startDate
            string url = string.Format("http://www.comingsoon.net/dvd/?year={0}&month={1}", startDate.Year, startDate.Month.ToString("00"));

            string html = Utils.GetHttpWebResponse(url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // List of titles by date
            //*[@id="col1"]/div/div[2]/div[@class='lstEntry_dvd']
            var dateNodes = document.DocumentNode.SelectNodes("//*[@id='col1']/div/div[2]/div[@class='lstEntry_dvd']");
            bool correctDate = false;
            foreach (var dateNode in dateNodes)
            {
                //string className = dateNode.ParentNode.Attributes["class"].Value.Trim();
                //if (className != "lstEntry_dvd")
                //{
                //    continue;
                //}
                //if (dateNode.InnerText == "Pre-Order")
                //{
                //    continue;
                //}

                string text = dateNode.InnerText.Trim();
                if (text.StartsWith(startDate.ToString("MMMM d")))
                {
                    correctDate = true;
                }
                if (text.StartsWith(endDate.ToString("MMMM d")))
                {
                    break;
                }

                if (!correctDate)
                {
                    continue;
                }

                var titleNode = GetTitleNode(dateNode.InnerHtml);
                if (titleNode == null)
                    continue;

                Console.WriteLine("Parsing '{0}'", titleNode.InnerText);
                string href = titleNode.Attributes["href"].Value.Trim();
                string id = ParseID(href);
                var feedTitle = new MovieTitle
                {
                    Id = id,
                    Name = titleNode.InnerText.Trim(),
                    Url = href,
                    Synopsis = string.Empty,
                    Cast = new List<MoviePerson>(),
                    Directors = new List<MoviePerson>(),
                    Genres = new List<string>(),
                    BoxArt = string.Empty,
                    ReleaseYear = DateTime.Now.Year,
                    Rating = "N/A",
                    Runtime = 0
                };

                MovieTitle netflixTitle = ParseRssItem(feedTitle);
                var newTitle = new TitleViewModel(netflixTitle, netflixTitle.Id.StartsWith("Z:") ? "Amazon.com" : this.Name, base._sundayWeekOfDate);
                originalTitles.Add(newTitle);
            }

            return originalTitles;
        }

        private HtmlNode GetTitleNode(string p)
        {
            var document = new HtmlDocument();
            document.LoadHtml(p);

            return document.DocumentNode.SelectSingleNode("//a");
        }

        private string ParseID(string href)
        {
            if (href.Contains("www.amazon.com"))
            {
                return "Z:" + href.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[5].Trim();
            }
            else
            {
                return "C:" + href.Split("=".ToCharArray())[1].Trim();
            }
        }

        public Models.MovieTitle ParseRssItem(Models.MovieTitle title)
        {
            if (title.Id.StartsWith("Z:"))
                return ParseAmazonRssItem(title);

            string html = Utils.GetHttpWebResponse(title.Url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // Information paragraph (that's all we get on this site)
            //*[@id="subPageContent"]/div/p
            var paraNode = document.DocumentNode.SelectSingleNode("//*[@id='subPageContent']/div/p");
            string para = paraNode == null ? string.Empty : paraNode.InnerText.Trim();

            var dict = ParseDescriptionParagraph(para);

            // Rating
            string rating = dict["MPAA Rating"];
            if (rating != "Not Available")
            {
                if (rating.Contains("("))
                {
                    title.Rating = rating.Substring(0, rating.IndexOf("(")).Trim().ToUpper();
                }
                else
                {
                    title.Rating = rating.Trim().ToUpper();
                }
            }

            // Director(s)
            string[] directorNodes = dict["Director"].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string directorNode in directorNodes)
            {
                string url = string.Empty;
                title.Directors.Add(new MoviePerson { Id = -1, Name = directorNode.Trim(), Url = url });
            }

            // Cast
            string[] castNodes = dict["Starring"].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string castNode in castNodes)
            {
                string url = string.Empty;
                title.Cast.Add(new MoviePerson { Id = -1, Name = castNode.Trim(), Url = url });
            }

            // Genres
            string[] genreNodes = dict["Genre"].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string genreNode in genreNodes)
            {
                title.Genres.Add(genreNode.Trim());
            }

            // Synopsis
            string synopsis = dict["Plot Summary"];
            title.Synopsis = synopsis.Trim();

            // Boxart
            //*[@id="subPageContent"]/div/a/img
            var boxartNode = document.DocumentNode.SelectSingleNode("//*[@id='subPageContent']/div/a/img");
            title.BoxArt = boxartNode == null ? string.Empty : boxartNode.Attributes["src"].Value;

            return title;
        }

        private MovieTitle ParseAmazonRssItem(MovieTitle title)
        {
            string html = Utils.GetHttpWebResponse(title.Url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // Detail nodes
            //*[@id="detail-bullets"]/table/tbody/tr/td/div/ul/li
            var itemNodes = document.DocumentNode.SelectNodes("//*[@id='detail-bullets']/table/tr/td/div/ul/li");

            var dict = ParseDetailListItems(itemNodes);

            // Reset the ID
            string id = dict["ASIN"];
            title.Id = "Z:" + id;

            // Release date
            string releaseDate = dict["DVD Release Date"];
            title.ReleaseYear = Convert.ToInt32(releaseDate.Substring(releaseDate.IndexOf(",") + 1));

            // Rating
            string rating = dict.ContainsKey("Rated") ? dict["Rated"] : "NR";
            if (rating == "Unrated")
            {
                rating = "NR";
            }
            else
            {
                rating = rating.IndexOf("(") == -1 ? rating : rating.Substring(0, rating.IndexOf("(")).Trim().ToUpper();
            }
            title.Rating = rating;

            // Running time
            string runtime = dict.ContainsKey("Run Time") ? dict["Run Time"] : "0";
            title.Runtime = ConvertRunningTime(runtime);

            // Director(s)
            if (dict.ContainsKey("Directors"))
            {
                string[] directorNodes = dict["Directors"].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string directorNode in directorNodes)
                {
                    string url = string.Empty;
                    title.Directors.Add(new MoviePerson { Id = -1, Name = directorNode.Trim(), Url = url });
                }
            }

            // Cast
            if (dict.ContainsKey("Actors"))
            {
                string[] castNodes = dict["Actors"].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string castNode in castNodes)
                {
                    string url = string.Empty;
                    title.Cast.Add(new MoviePerson { Id = -1, Name = castNode.Trim(), Url = url });
                }
            }

            // Editorial Review
            //*[@id="productDescription"]/div/div
            var synopsisNode = document.DocumentNode.SelectSingleNode("//*[@id='productDescription']/div/div");
            if (title.Synopsis.Length == 0 && synopsisNode != null)
            {
                title.Synopsis = synopsisNode.InnerText.Trim();
            }

            // BoxArt
            //*[@id="holderMainImage"]/noscript
            var boxartNode = document.DocumentNode.SelectSingleNode("//*[@id='holderMainImage']/noscript");
            title.BoxArt = ParseBoxArtHtml(boxartNode.InnerHtml);

            return title;
        }

        private string ParseBoxArtHtml(string p)
        {
            var document = new HtmlDocument();
            document.LoadHtml(p);

            return document.DocumentNode.ChildNodes[0].Attributes["src"].Value.Trim();
        }

        private Dictionary<string, string> ParseDetailListItems(HtmlNodeCollection itemNodes)
        {
            var dict = new Dictionary<string, string>();

            foreach (var item in itemNodes)
            {
                string valueToParse = item.InnerText.Trim();
                if (!valueToParse.Contains(":"))
                    continue;

                string[] kvp = valueToParse.Split(":".ToCharArray());
                string key = kvp[0].Trim();
                string value = kvp[1].Trim();
                dict.Add(key, value);
            }

            return dict;
        }

        private Dictionary<string, string> ParseDescriptionParagraph(string para)
        {
            var dict = new Dictionary<string, string>();
            string[] lines = para.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i += 2)
            {
                if (!lines[i].Contains(":"))
                    continue;

                string key = lines[i].Replace(":", "").Trim();
                if (key.Contains("Trailer") || key.Contains("Teaser") || key.Contains("Clip"))
                    break;
                string value = lines[i + 1].Trim();
                dict.Add(key, value);
            }

            return dict;
        }

        private int ConvertRunningTime(string runtime)
        {
            string[] values = runtime.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            //int hours = Convert.ToInt32(values[1]);
            int minutes = Convert.ToInt32(values[0]);

            return minutes;
        }
    }
}
