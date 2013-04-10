using System;
using System.Collections.Generic;

namespace Reflix.Models
{
    public class TitleCache
    {
        public int TitleID { get; set; }
        public System.DateTime WeekOfDate { get; set; }
        public string ObjectData { get; set; }
        public bool IsRss { get; set; }
    }
}
