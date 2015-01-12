using Reflix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflix.SiteParsing
{
    public interface ICustomSiteParser
    {
        string Name { get; }
        List<TitleViewModel> ParseRssList();
        MovieTitle ParseRssItem(MovieTitle title);
    }
}
