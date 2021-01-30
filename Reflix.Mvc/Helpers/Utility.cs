using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NCI.Utility
{
    internal class Utils
    {
        public static string GetHttpWebResponse(string url, string postData = null, CookieContainer cookieContainer = null)
        {
            ServicePointManager.Expect100Continue = false;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.4 (KHTML, like Gecko) Chrome/22.0.1229.96 Safari/537.4";
            request.Headers.Set("Accept-Encoding", "gzip,deflate,sdch");
            request.Headers.Set("Accept-Language", "en-US,en;q=0.8");
            request.Headers.Set("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3");
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Set("Cache-Control", "max-age=0");
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 5;

            if (cookieContainer != null)
                ((HttpWebRequest)request).CookieContainer = cookieContainer;

            if (!string.IsNullOrEmpty(postData))
            {
                byte[] send = Encoding.Default.GetBytes(postData);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = send.Length;

                using (Stream sout = request.GetRequestStream())
                {
                    sout.Write(send, 0, send.Length);
                    sout.Flush();
                    sout.Close();
                }
            }

            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Transform the incoming url to an MD5 hash code
        /// </summary>
        /// <param name="url">The url to transform</param>
        /// <returns>A 32 character hash code</returns>
        public static string CreateMD5Hash(string url)
        {
            char[] cs = url.ToLowerInvariant().ToCharArray();
            byte[] buffer = new byte[cs.Length];
            for (int i = 0; i < cs.Length; i++)
                buffer[i] = (byte)cs[i];

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] output = md5.ComputeHash(buffer);

            var sb = new StringBuilder();
            for (int i = 0; i < output.Length; i++)
                sb.AppendFormat("{0:x2}", output[i]);

            return sb.ToString();
        }

        public static DateTime CalculateStartDate()
        {
            DateTime testDate = DateTime.Now.Date;

            //if (testDate.Day == (int)DayOfWeek.Sunday)
            //    return testDate;

            return testDate.AddDays(-(int)testDate.DayOfWeek);
        }
    }
}
