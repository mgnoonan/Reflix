using HtmlAgilityPack;
using log4net;
using Reflix.Models;
using Reflix.SiteParsing.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflix.SiteParsing
{
    public class DvdsReleaseDatesSiteParser : BaseSiteParser, ICustomSiteParser
    {
        public DvdsReleaseDatesSiteParser(string url, DateTime startDate, string name, ILog log) : base(url, startDate, name, log) { }

        public string Name { get { return base._sourceName; } }

        public List<TitleViewModel> ParseRssList()
        {
            var originalTitles = new List<TitleViewModel>();

            // Determine the correct URL for the given startDate
            string url = "http://www.dvdsreleasedates.com/";

            string html = Utils.GetHttpWebResponse(url, null, new System.Net.CookieContainer());
            var document = new HtmlDocument();
            document.LoadHtml(html);

            // List of titles by date
            //*[@id="leftcolumn"]/table/tbody/tr[10]/td/table
            var rows = document.DocumentNode.SelectNodes("//*[@id=\"leftcolumn\"]/table/tbody/tr");
            var selectedRow = rows.Skip(rows.Count() - 1).SingleOrDefault();

            //*[@id="leftcolumn"]/table/tbody/tr[10]/td/table/tbody/tr[3]/td[1]/a[2]

            return originalTitles;
        }

        public MovieTitle ParseRssItem(MovieTitle title)
        {
            throw new NotImplementedException();
        }
    }
}
