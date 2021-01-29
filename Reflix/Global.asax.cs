using System;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Raven.Client.Documents;
using Raven.Embedded;

namespace Reflix
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static IDocumentStore _store;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            InitializeRavenDB("~/App_Data", true);
        }

        private static bool InitializeRavenDB(string dataDirectory, bool rethrowException)
        {
            EmbeddedServer.Instance.StartServer();

            try
            {
                _store = EmbeddedServer.Instance.GetDocumentStore("Embedded");
                _store.Initialize();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
                if (rethrowException)
                    throw;

                return false;
            }
        }
    }
}
