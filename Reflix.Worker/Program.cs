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
            Console.WriteLine("\nRSSFeed");
            Console.WriteLine("------------\n");

            try
            {
                // Open the logfile
                logfile.Open("RSSFeed.log");
                logfile.ConsoleOutput = true;
                logfile.WriteLine("RSSFeed starting");

                DateTime targetDate = Utils.CalculateStartDate();
                var parsers = new List<ICustomSiteParser>();
                //parsers.Add(new NetflixSiteParser("http://rss.netflix.com/NewReleasesRSS", targetDate, "Netflix"));
                parsers.Add(new MoviesDotComSiteParser("http://www.movies.com/rss-feeds/new-on-dvd-rss", targetDate, "Movies.com"));

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
                Console.WriteLine("{0} {1}: {2}", title.RssWeekNumber, title.RssWeekOf, title.Title);
            }
        }

        private static void AddNewTitles(DateTime targetDate, List<ICustomSiteParser> parsers)
        {
            Console.WriteLine("Retrieving existing titles for {0}", targetDate);
            var existingTitles = GetExistingTitles(targetDate);
            PrintTitles(existingTitles);

            var newTitles = new List<TitleViewModel>();
            foreach (var parser in parsers)
            {
                Console.WriteLine("\nRetrieving new titles for {0}", parser.Name);
                var list = parser.ParseRssList();
                newTitles.AddRange(list.AsEnumerable());
            }

            foreach (var title in newTitles)
            {
                if (existingTitles.Any(e => e.Title.Id == title.Title.Id))
                    continue;

                var client = new RestClient("http://localhost:4204/api");
                var request = new RestRequest("Title", Method.POST);
                request.RequestFormat = DataFormat.Json;
                request.AddBody(title);
                //var response = client.Execute<TitleViewModel>(request);
                var response = client.Post<TitleViewModel>(request);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception(string.Format("Error {0}", response.StatusCode));
                }
            }
        }

        private static List<TitleViewModel> GetExistingTitles(DateTime targetDate)
        {
            var client = new RestClient("http://localhost:4204/api");
            var request = new RestRequest("Title", Method.GET);
            //request.AddParameter("targetDate", targetDate.ToString("yyyy-MM-dd"));

            var response = client.Execute(request);
            string responseString = response.Content;
            var oldTitles = JsonConvert.DeserializeObject<List<TitleViewModel>>(responseString);
            return oldTitles;
        }
    }
}
