using log4net;
using Newtonsoft.Json;
using Reflix.Models;
using Reflix.SiteParsing;
using Reflix.Worker.Utility;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Reflix.Worker
{
    class Program
    {
        /// <summary>
        /// Instance of the log4net logger
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            // Init the log4net through the confi
            log4net.Config.XmlConfigurator.Configure();
            log.Info("--------------------------------");

            try
            {
                log.Info("Starting");

                DateTime targetDate = Utils.CalculateStartDate();
                //DateTime targetDate = new DateTime(2014, 5, 4);

                // Add the current supported parsers (they come and go)
                var parsers = new List<ICustomSiteParser>();
                parsers.Add(new NetflixSiteParser("http://rss.netflix.com/NewReleasesRSS", targetDate, "Netflix", log));
                parsers.Add(new MoviesDotComSiteParser("http://www.movies.com/rss-feeds/new-on-dvd-rss", targetDate, "Movies.com", log));

                // Deprecated
                //parsers.Add(new DvdsReleaseDatesSiteParser("http://www.dvdsreleasedates.com/", targetDate, "dvdsreleasedates.com"));
                //parsers.Add(new ComingSoonNetSiteParser("http://www.commingsoon.net/dvd", targetDate, "ComingSoon.net"));
                //parsers.Add(new BlockbusterSiteParser("http://www.blockbuster.com/rss/newRelease", targetDate, "Blockbuster"));

                AddNewTitles(targetDate, parsers);
                log.Info("Completed successfully");
            }
            catch (Exception ex)
            {
                log.Error("Unexpected error", ex);
                throw;
            }

#if DEBUG
            Console.WriteLine("\nPress <ENTER> to continue...");
            Console.ReadLine();
#endif

            Console.WriteLine();
        }

        private static void PrintTitles(List<TitleViewModel> titles)
        {
            foreach (var title in titles)
            {
                log.InfoFormat("{0} [{1}]: {2}", title.RssWeekNumber, title.Title.Id, title.Title.Name);
            }
        }

        private static void AddNewTitles(DateTime targetDate, List<ICustomSiteParser> parsers)
        {
            log.InfoFormat("Retrieving existing titles for {0:yyyy-MM-dd}", targetDate);
            var existingTitles = GetExistingTitles(targetDate);
            PrintTitles(existingTitles);

            var newTitles = new List<TitleViewModel>();
            foreach (var parser in parsers)
            {
                log.InfoFormat("Retrieving new titles for {0}", parser.Name);
                var list = parser.ParseRssList();
                newTitles.AddRange(list.AsEnumerable());
            }

            log.Info("Saving new titles");
            foreach (var title in newTitles)
            {
                if (existingTitles.Any(e => e.Title.Id == title.Title.Id) ||
                    existingTitles.Any(e => e.Title.Name == title.Title.Name))
                    continue;

#if DEBUG
                var client = new RestClient("http://localhost:4204/api");
#else
                var client = new RestClient("http://reflix.azurewebsites.net/api");
#endif
                var request = new RestRequest("Title", Method.POST);
                request.RequestFormat = DataFormat.Json;
                request.AddBody(title);

                try
                {
                    log.InfoFormat("Posting new title '{0}'", title.Title.Name);
                    var response = client.Post<TitleViewModel>(request);

                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        existingTitles.Add(title);
                    }
                    else
                    {
                        log.WarnFormat("Response code '{0}' does not indicate success", response.StatusCode);
                    }
                }
                catch(Exception ex)
                {
                    // Think we should bail if we get an error here
                    log.Error("Error retrieving title from service", ex);
                    throw;
                }
            }
        }

        private static List<TitleViewModel> GetExistingTitles(DateTime targetDate)
        {
#if DEBUG
            var client = new RestClient("http://localhost:4204/api");
#else
            var client = new RestClient("http://reflix.azurewebsites.net/api");
#endif
            var request = new RestRequest("Title", Method.GET);
            request.AddParameter("targetDate", targetDate.Date.ToString("yyyy-MM-dd"));

            var response = client.Execute(request);
            string responseString = response.Content;
            var oldTitles = JsonConvert.DeserializeObject<List<TitleViewModel>>(responseString);
            return oldTitles;
        }
    }
}
