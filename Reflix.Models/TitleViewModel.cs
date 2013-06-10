using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Reflix.Models
{
    public class TitleViewModel
    {
        public TitleViewModel(MovieTitle title, bool isRss, DateTime rssDate)
        {
            this.Title = title;
            this.IsRss = isRss;
            this.RssDate = rssDate;
        }

        public TitleViewModel()
        {
            // TODO: Complete member initialization
        }

        public MovieTitle Title { get; set; }
        public bool IsRss { get; set; }
        public DateTime RssDate { get; set; }
    }
}