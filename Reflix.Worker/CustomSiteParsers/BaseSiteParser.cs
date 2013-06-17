using HtmlAgilityPack;
using Reflix.Models;
using Reflix.Worker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflix.Worker.CustomSiteParsers
{
    public class BaseSiteParser
    {
        protected string _url;
        protected DateTime _startDate;
        protected string _name;

        public BaseSiteParser(string url, DateTime startDate, string name)
        {
            _url = url;
            _startDate = startDate;
            _name = name;
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

            //*[@id="nmmdp"]/table/tr/td/div/div[1]/span
            var nodes = document.DocumentNode.SelectNodes("//*[@id='nmmdp']/table/tr/td/div/div[1]/span");
            title.ReleaseYear = Convert.ToInt32(nodes[0].InnerText);
            title.Rating = nodes[1] == null ? "N/A" : nodes[1].InnerText;

            if (nodes.Count >= 3)
            {
                string runtime = nodes[2] == null ? "0 " : nodes[2].InnerText;
                title.Runtime = Convert.ToInt32(runtime.Substring(0, runtime.IndexOf(' ')).Trim());
            }

            try
            {
                //*[@id="support"]/div[1]/a
                nodes = document.DocumentNode.SelectNodes("//*[@id='support']/div[1]/a");
                foreach (var node in nodes)
                {
                    string url = node.Attributes["href"].Value.Trim();
                    string parsedID = url.Substring(url.LastIndexOf('/') + 1);
                    title.Cast.Add(new MoviePerson { Id = Convert.ToInt32(parsedID), Name = node.InnerText.Trim(), Url = url });
                }
            }
            catch { }

            try
            {
                //*[@id="support"]/div[2]/a
                nodes = document.DocumentNode.SelectNodes("//*[@id='support']/div[2]/a");
                foreach (var node in nodes)
                {
                    string url = node.Attributes["href"].Value.Trim();
                    string parsedID = url.Substring(url.LastIndexOf('/') + 1);
                    title.Directors.Add(new MoviePerson { Id = Convert.ToInt32(parsedID), Name = node.InnerText.Trim(), Url = url });
                }
            }
            catch { }

            try
            {
                //*[@id="support"]/div[3]/a
                nodes = document.DocumentNode.SelectNodes("//*[@id='support']/div[3]");
                foreach (var node in nodes)
                {
                    title.Genres.Add(node.InnerText.Replace("Genre:", string.Empty).Trim());
                }
            }
            catch { }

            return title;
        }
    }
}
