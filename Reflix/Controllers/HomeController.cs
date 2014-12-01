using NCI.Utility;
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
                    calculatedStartDate = Utils.CalculateStartDate();
                else
                    calculatedStartDate = startDate.Value.Date;

                var calculatedEndDate = calculatedStartDate.AddDays(6);

                var modelList = GetRssTitlesFromEmbeddedStore(calculatedStartDate, calculatedEndDate);

                ViewBag.Message = string.Format("Week of {0}", calculatedStartDate.ToString("d-MMM-yyyy"));
                ViewBag.StartDate = calculatedStartDate;
                ViewBag.EndDate = calculatedEndDate;
                ViewBag.DisplayAll = false;

                return View("Index", modelList.OrderBy(t => t.Title.Name).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
                throw;
            }
        }

        public ActionResult ListAll()
        {
            try
            {
                var modelList = GetRssTitlesFromEmbeddedStore();

                ViewBag.Message = "All entries";
                ViewBag.StartDate = DateTime.MaxValue;
                ViewBag.EndDate = DateTime.MaxValue;
                ViewBag.DisplayAll = true;

                return View("Index", modelList.OrderBy(t => t.Title.Name).ToList());
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

        private List<TitleViewModel> GetRssTitlesFromEmbeddedStore(DateTime newStartDate, DateTime newEndDate)
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        where title.RssWeekOf >= newStartDate && title.RssWeekOf <= newEndDate
                        select title;

            return query.ToList();
        }

        private List<TitleViewModel> GetRssTitlesFromEmbeddedStore()
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        //where title.RssWeekOf >= newStartDate && title.RssWeekOf <= newEndDate
                        select title;

            return query.ToList();
        }

        private void DeleteRssTitle(string id)
        {
            var item = (from title in this.RavenSession.Query<TitleViewModel>()
                       where title.Title.Id == id
                       select title).SingleOrDefault();

            this.RavenSession.Delete(item);
        }
    }
}
