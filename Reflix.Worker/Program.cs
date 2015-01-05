using Newtonsoft.Json;
using Reflix.Models;
using Reflix.Worker.CustomSiteParsers;
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
        private static Log logfile = new Log();

        static void Main(string[] args)
        {
            // Print banner to console
            Console.WriteLine("\nReflix.Worker");
            Console.WriteLine("------------\n");

            try
            {
                // Open the logfile
                logfile.Open("Reflix.Worker.log");
                logfile.ConsoleOutput = true;
                logfile.WriteLine("Reflix.Worker starting");

                DateTime targetDate = Utils.CalculateStartDate();
                //DateTime targetDate = new DateTime(2014, 5, 4);

                // Add the current supported parsers (they come and go)
                var parsers = new List<ICustomSiteParser>();
                parsers.Add(new NetflixSiteParser("http://rss.netflix.com/NewReleasesRSS", targetDate, "Netflix"));
                parsers.Add(new MoviesDotComSiteParser("http://www.movies.com/rss-feeds/new-on-dvd-rss", targetDate, "Movies.com"));
                //parsers.Add(new DvdsReleaseDatesSiteParser("http://www.dvdsreleasedates.com/", targetDate, "dvdsreleasedates.com"));

                // Deprecated
                //parsers.Add(new ComingSoonNetSiteParser("http://www.commingsoon.net/dvd", targetDate, "ComingSoon.net"));
                //parsers.Add(new BlockbusterSiteParser("http://www.blockbuster.com/rss/newRelease", targetDate, "Blockbuster"));

                AddNewTitles(targetDate, parsers);
            }
            //catch (DbEntityValidationException dbEx)
            //{
            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //    {
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //        {
            //            logfile.WriteLine("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
            //        }
            //    }
            //}
            catch (Exception ex)
            {
                logfile.WriteLine(ex.Message + ex.StackTrace);
                throw;
            }
            finally
            {
                // Clean up log and screen display
                logfile.Close();
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
                Console.WriteLine("{0} [{1}]: {2}", title.RssWeekNumber, title.Title.Id, title.Title.Name);
            }
        }

        private static void AddNewTitles(DateTime targetDate, List<ICustomSiteParser> parsers)
        {
            Console.WriteLine("Retrieving existing titles for {0:yyyy-MM-dd}", targetDate);
            var existingTitles = GetExistingTitles(targetDate);
            PrintTitles(existingTitles);

            var newTitles = new List<TitleViewModel>();
            foreach (var parser in parsers)
            {
                Console.WriteLine("\nRetrieving new titles for {0}", parser.Name);
                var list = parser.ParseRssList();
                newTitles.AddRange(list.AsEnumerable());
            }

            Console.WriteLine("\nSaving new titles");
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

                Console.WriteLine("Posting new title '{0}'", title.Title.Name);
                var response = client.Post<TitleViewModel>(request);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    existingTitles.Add(title);
                }
                else
                {
                    throw new Exception(string.Format("Error {0}", response.StatusCode));
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
