using HtmlAgilityPack;
using Reflix.Models;
using Reflix.SiteParsing.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Reflix.SiteParsing
{
    public class BaseSiteParser
    {
        protected string _sourceUrl;
        protected DateTime _sundayWeekOfDate;
        protected string _sourceName;

        public BaseSiteParser(string url, DateTime startDate, string name)
        {
            _sourceUrl = url;
            _sundayWeekOfDate = startDate;
            _sourceName = name;
        }

        public MovieTitle SearchNetflixTitle(MovieTitle title)
        {
            string url = "http://dvd.netflix.com/Search?v1=" + title.Name.Replace(" ", "+");
            string html = Utils.GetHttpWebResponse(url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            //*[@id="searchResultsPrimaryWrapper"]/ol/li[1]/div
            var titleHeaderNode = document.DocumentNode.SelectSingleNode("//*[@id='searchResultsPrimaryWrapper']/ol/li[1]/div");
            if (titleHeaderNode == null)
                return title;

            string netflixID = titleHeaderNode.Attributes["id"].Value;
            int startIndex = 0;
            int endIndex = netflixID.LastIndexOf("_");
            int len = endIndex - startIndex;
            title.Id = netflixID.Substring(startIndex, len);

            return ParseNetflixTitle(title);
        }

        public MovieTitle ParseNetflixTitle(MovieTitle title)
        {
            string html = Utils.GetHttpWebResponse(title.Url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            ParseMetaData(title, document);
            ParseCast(title, document, "//*[@id=\"mdp-details\"]/div[1]/div[1]/dl/dd/a", "Cast", title.Cast);
            ParseCast(title, document, "//*[@id=\"mdp-details\"]/div[1]/div[2]/dl/dd/a", "Director", title.Directors);
            ParseGenre(title, document, "//*[@id=\"mdp-details\"]/div[1]/div[3]/dl/dd/a", "Genre", title.Genres);

            return title;
        }

        private void ParseGenre(MovieTitle title, HtmlDocument document, string xpath, string castType, List<string> list)
        {
            try
            {
                var nodes = document.DocumentNode.SelectNodes(xpath);
                if (nodes == null)
                {
                    Console.WriteLine("No valid {0} nodes found", castType);
                    return;
                }

                foreach (var node in nodes)
                {
                    string name = HttpUtility.HtmlDecode(node.InnerText.Replace("Genre:", string.Empty).Trim());
                    Console.WriteLine("Genre: {0}", name);
                    title.Genres.Add(name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ParseCast(MovieTitle title, HtmlDocument document, string xpath, string castType, List<MoviePerson> list)
        {
            try
            {
                //*[@id="support"]/div[1]/a
                //*[@id="mdp-details"]/div[1]/div[1]/dl/dd/a
                var nodes = document.DocumentNode.SelectNodes(xpath);
                if (nodes == null)
                {
                    Console.WriteLine("No valid {0} nodes found", castType);
                    return;
                }

                foreach (var node in nodes)
                {
                    string url = node.Attributes["href"].Value.Trim();
                    int id = ParseIdFromUrl(url);
                    string name = HttpUtility.HtmlDecode(node.InnerText.Trim());
                    Console.WriteLine("{0}: {1}", castType, name);
                    list.Add(new MoviePerson { Id = id, Name = name, Url = url });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private int ParseIdFromUrl(string url)
        {
            int startIndex = url.LastIndexOf('/') + 1;
            int length = url.LastIndexOf('?') - startIndex;

            string parsedID = length < 0 ? url.Substring(startIndex) : url.Substring(startIndex, length);

            int id = 0;
            if (int.TryParse(parsedID, out id))
            {
                return id;
            }

            return 0;
        }

        private void ParseMetaData(MovieTitle title, HtmlDocument document)
        {
            //*[@id="nmmdp"]/table/tr/td/div/div[1]/span
            //*[@id="mdp-metadata-container"]/span
            var nodes = document.DocumentNode.SelectNodes("//*[@id=\"mdp-metadata-container\"]/span");

            if (nodes == null)
            {
                title.ReleaseYear = DateTime.Now.Year;
                title.Rating = "N/A";
                title.Runtime = 0;
                return;
            }

            int releaseYear = 0;
            if (int.TryParse(nodes[0].InnerText, out releaseYear))
            {
                title.ReleaseYear = releaseYear;
            }
            else
            {
                title.ReleaseYear = DateTime.Now.Year;
            }
            Console.WriteLine("Release Year: {0}", title.ReleaseYear);

            title.Rating = nodes[1] == null ? "N/A" : nodes[1].InnerText.Trim();
            Console.WriteLine("Rating: {0}", title.Rating);

            if (nodes.Count >= 3)
            {
                string runtimeText = nodes[2] == null || string.IsNullOrWhiteSpace(nodes[2].InnerText) ? "0 " : nodes[2].InnerText;
                runtimeText = runtimeText.Substring(0, runtimeText.IndexOf(' '));
                int runtime = 0;
                if (int.TryParse(runtimeText, out runtime))
                {
                    title.Runtime = runtime;
                }
                else
                {
                    title.Runtime = 0;
                }
            }
            Console.WriteLine("Runtime: {0}", title.Runtime);
        }
    }
}
