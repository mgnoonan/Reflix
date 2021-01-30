using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Session;

namespace Reflix.Mvc.Controllers
{
    public abstract class BaseRavenApiController : Controller
    {
        public IDocumentSession RavenSession { get; protected set; }

        //protected override void Initialize(HttpControllerContext controllerContext)
        //{
        //    base.(controllerContext);
        //    //if (RavenSession == null)
        //    //    RavenSession = MvcApplication._store.OpenSession();
        //}

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            using (RavenSession)
            {
                if (RavenSession != null)
                    RavenSession.SaveChanges();
            }
        }
    }
}