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

                DateTime startDate = DateTime.Now.Date;
                var parsers = new List<ICustomSiteParser>();
                parsers.Add(new NetflixSiteParser("http://rss.netflix.com/NewReleasesRSS", startDate));
                //parsers.Add(new MoviesDotComSiteParser("http://www.movies.com/rss-feeds/new-on-dvd-rss", startDate));

                AddNewTitles(parsers);

                //var oldTitles = GetExistingTitles();
                //foreach (var title in oldTitles)
                //{
                //    Console.WriteLine("Item: {0}", title.Title.Url);
                //}
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

        private static void AddNewTitles(List<ICustomSiteParser> parsers)
        {
            var newTitles = new List<TitleViewModel>();
            foreach (var parser in parsers)
            {
                var list = parser.ParseRssList();
                newTitles.AddRange(list.AsEnumerable());
            }

            foreach (var title in newTitles)
            {
                var client = new RestClient("http://localhost:4204/api");
                var request = new RestRequest("Title", Method.POST);
                request.RequestFormat = DataFormat.Json;
                request.AddParameter("title", title);
                //var response = client.Execute<TitleViewModel>(request);
                var response = client.Post<TitleViewModel>(request);

                if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                {
                    throw response.ErrorException;
                }
            }
        }

        private static List<TitleViewModel> GetExistingTitles()
        {
            var client = new RestClient("http://localhost:4204/api");
            var request = new RestRequest("Title", Method.GET);
            var response = client.Execute(request);
            string responseString = response.Content;
            var oldTitles = JsonConvert.DeserializeObject<List<TitleViewModel>>(responseString);
            return oldTitles;
        }
    }
}
