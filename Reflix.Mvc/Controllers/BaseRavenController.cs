using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raven.Client.Documents.Session;

namespace Reflix.Mvc.Controllers
{
    public class BaseRavenController : Controller
    {
        protected IDocumentSession RavenSession { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //RavenSession = MvcApplication._store.OpenSession();

            //filterContext.Controller.ViewBag.CurrentAssemblyVersion = string.Format("v. {0}", this.CurrentAssemblyVersion);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //if (filterContext.IsChildAction)
            //    return;

            using (RavenSession)
            {
                if (filterContext.Exception != null)
                    return;

                if (RavenSession != null)
                    RavenSession.SaveChanges();
            }
        }

        /// <summary>
        /// Returns the current assembly version
        /// </summary>
        protected virtual string CurrentAssemblyVersion
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }
    }
}
