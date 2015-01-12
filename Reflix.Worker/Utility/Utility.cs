using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Reflix.Worker.Utility
{
    /// <summary>
    /// Summary description for Utility.
    /// </summary>
    internal class Utils
    {
        public static DateTime CalculateStartDate()
        {
            DateTime testDate = DateTime.Now.Date;

            //if (testDate.Day == (int)DayOfWeek.Sunday)
            //    return testDate;

            return testDate.AddDays(-(int)testDate.DayOfWeek);
        }
    }
}
