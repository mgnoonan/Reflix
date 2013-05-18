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

        // POST api/title
        public HttpResponseMessage Post(TitleViewModel value)
        {
            if (this.ModelState.IsValid)
            {
                this.RavenSession.Store(value);
                var response = Request.CreateResponse<TitleViewModel>(HttpStatusCode.Created, value);
                response.Headers.Location = GetTitleLocation(value.Title.Id);
                return response;
            }

            return Request.CreateResponse(HttpStatusCode.BadRequest);
        }

        // PUT api/title/5
        public void Put(int id, [FromBody]TitleViewModel value)
        {
            throw new NotImplementedException();
        }

        // DELETE api/title/5
        public void Delete(int id)
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
