using Reflix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Reflix.Controllers
{
    public class TitleController : BaseRavenApiController
    {
        // GET api/title
        public IEnumerable<TitleViewModel> Get()
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        select title;

            return query.AsEnumerable();
        }

        // GET api/title/5
        public TitleViewModel Get(string id)
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        where title.Title.Id == id
                        select title;

            return query.SingleOrDefault();
        }

        // GET api/title/getbytargetdate
        public IEnumerable<TitleViewModel> GetByTargetDate(DateTime targetDate)
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        where title.RssWeekOf == targetDate
                        select title;

            return query.AsEnumerable();
        }

        // POST api/title
        public HttpResponseMessage Post(TitleViewModel title)
        {
            if (this.ModelState.IsValid)
            {
                this.RavenSession.Store(title);
                var response = Request.CreateResponse<TitleViewModel>(HttpStatusCode.Created, title);
                response.Headers.Location = GetTitleLocation(title.Title.Id);
                return response;
            }

            return Request.CreateResponse(HttpStatusCode.BadRequest);
        }

        // PUT api/title/5
        public void Put(string id, TitleViewModel value)
        {
            throw new NotImplementedException();
        }

        // DELETE api/title/5
        public void Delete(string id)
        {
            throw new NotImplementedException();
        }

        private Uri GetTitleLocation(string id)
        {
            var controller = this.Request.GetRouteData().Values["controller"];
            return new Uri(this.Url.Link("DefaultApi", new { controller = controller, id = id }));
        }

    }
}
