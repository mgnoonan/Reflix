using System.Web.Http;
using Raven.Client.Documents.Session;

namespace Reflix.Controllers
{
    public abstract class BaseRavenApiController : ApiController
    {
        public IDocumentSession RavenSession { get; protected set; }

        protected override void Initialize(System.Web.Http.Controllers.HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            if (RavenSession == null)
                RavenSession = MvcApplication._store.OpenSession();
        }

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