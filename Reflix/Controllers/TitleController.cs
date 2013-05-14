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

        //// POST api/title
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/title/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/title/5
        //public void Delete(int id)
        //{
        //}
    }
}
