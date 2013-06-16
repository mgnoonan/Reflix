using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Reflix.Models
{
    public class TitleViewModel
    {
        public TitleViewModel(MovieTitle title, string source, DateTime weekOfDate)
        {
            this.Title = title;
            this.Source = source;
            this.IsRss = true;
            this.RssWeekOf = weekOfDate.Date;
            this.RssWeekNumber = GetWeekNumber(weekOfDate.Date);
        }

        public TitleViewModel()
        {
            // TODO: Complete member initialization
        }

        public MovieTitle Title { get; set; }
        public bool IsRss { get; set; }
        public string Source { get; set; }
        public DateTime RssWeekOf { get; set; }
        public short RssWeekNumber { get; set; }

        public static short GetWeekNumber(DateTime targetDate)
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            short weekNumber = (short)currentCulture.Calendar.GetWeekOfYear(targetDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);

            return weekNumber;
        }
    }
}