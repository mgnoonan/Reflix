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

        public BaseSiteParser(string url, DateTime startDate)
        {
            _url = url;
            _startDate = startDate;
        }

        public Title ParseNetflixTitle(Title title)
        {
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
    }
}
