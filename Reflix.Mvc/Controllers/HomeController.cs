using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NCI.Utility;
using Reflix.Models;

namespace Reflix.Mvc.Controllers
{
    public class HomeController : BaseRavenController
    {
        public IActionResult Index(DateTime? startDate)
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

        public IActionResult NgIndex()
        {
            return View();
        }

        public IActionResult ListAll()
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

        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index");
            }

            id = id.Replace("_", ":");

            var model = GetRssTitleFromEmbeddedStore(id);

            ViewBag.StartDate = model.RssWeekOf.ToString("yyyy-MM-dd");

            return View(model.Title);
        }

        public IActionResult Update(string id)
        {
            id = id.Replace("_", ":");

            var model = GetRssTitleFromEmbeddedStore(id);

            ViewBag.StartDate = model.RssWeekOf.ToString("yyyy-MM-dd");

            return View(model.Title);
        }

        public IActionResult About()
        {
            ViewBag.Message = "Data sources:";

            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Your useless contact page.";

            return View();
        }

        private TitleViewModel GetRssTitleFromEmbeddedStore(string id)
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        where title.Title.Id == id
                        select title;

            return query.FirstOrDefault();
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
