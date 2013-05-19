using Reflix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflix.Worker.CustomSiteParsers
{
    interface ICustomSiteParser
    {
        List<TitleViewModel> ParseRssList();
        Title ParseRssItem(Title title);
    }
}
