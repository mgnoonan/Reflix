using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Reflix.Models;

namespace Reflix.Mvc.Controllers
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

        // GET api/title?targetDate=yyyy-MM-dd
        public IEnumerable<TitleViewModel> GetByTargetDate(DateTime targetDate)
        {
            var query = from title in this.RavenSession.Query<TitleViewModel>()
                        where title.RssWeekOf >= targetDate.Date && title.RssWeekOf <= targetDate.Date.AddDays(1)
                        select title;

            return query.AsEnumerable();
        }

        // POST api/title
        public IActionResult Post(TitleViewModel model)
        {
            throw new NotImplementedException();
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
    }
}
