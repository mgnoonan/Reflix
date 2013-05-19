using Reflix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Reflix.Controllers
{
    public class HomeController : BaseRavenController
    {
        public ActionResult Index(DateTime? startDate)
        {
            try
            {
                DateTime calculatedStartDate;
                if (!startDate.HasValue)
                    calculatedStartDate = CalculateStartDate();
                else
                    calculatedStartDate = startDate.Value.Date;

                var calculatedEndDate = calculatedStartDate.AddDays(6);

                var modelList = GetRssTitlesFromEmbeddedStore(calculatedStartDate, calculatedEndDate);

                ViewBag.Message = string.Format("Week of {0}", calculatedStartDate.ToString("d-MMM-yyyy"));
                ViewBag.StartDate = calculatedStartDate;
                ViewBag.EndDate = calculatedEndDate;

                return View("IndexMBS", modelList.OrderBy(t => t.Title.Name).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
                throw;
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Data sources:";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private DateTime CalculateStartDate()
        {
            DateTime testDate = DateTime.Now.Date;

            //if (testDate.Day == (int)DayOfWeek.Sunday)
            //    return testDate;

            return testDate.AddDays(-(int)testDate.DayOfWeek);
        }

        private List<TitleViewModel> GetRssTitlesFromEmbeddedStore(DateTime newStartDate, DateTime newEndDate)
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        where title.RssDate >= newStartDate && title.RssDate <= newEndDate
                        select title;

            return query.ToList();
        }
    }
}
