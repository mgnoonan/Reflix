using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using System.Web;

namespace Reflix.Worker.Utility
{
    /// <summary>
    /// Summary description for Utility.
    /// </summary>
    internal class Utils
    {
        public static string CleanString(string value, string pattern)
        {
            return CleanString(value, pattern, string.Empty);
        }

        public static string CleanString(string value, string pattern, string replacement)
        {
            return CleanString(value, pattern, string.Empty, RegexOptions.IgnoreCase);
        }

        public static string CleanString(string value, string pattern, string replacement, RegexOptions options)
        {
            return Regex.Replace(value, pattern, replacement, options);
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

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < output.Length; i++)
                builder.AppendFormat("{0:x2}", output[i]);

            return builder.ToString();
        }

        public static string GetParagraphTagsFromHtml(string url)
        {
            string description = string.Empty;

            try
            {
                // Load the initial HTML from the URL
                HtmlWeb hw = new HtmlWeb();
                HtmlDocument doc = hw.Load(url);

                // Sanitize the markup and reload back to the HtmlDocument
                var sanitizedMarkup = MarkupSanitizer.Sanitizer.SanitizeMarkup(doc.DocumentNode.InnerHtml);
                doc.LoadHtml(sanitizedMarkup.MarkupText);

                // Grab all the paragraph tags and just append the innnerText
                var paragraphTags = doc.DocumentNode.SelectNodes("//p");
                foreach(var paragraph in paragraphTags)
                {
                    string innerText = HttpUtility.HtmlDecode(paragraph.InnerText).Trim();
                    innerText = RemoveWhitespaceWithSplit(innerText).Trim();
                    if (!string.IsNullOrWhiteSpace(innerText) && CharCount(innerText, ";{}=|") < 3 && CharCount(innerText, ' ') >= 5)
                    {                        
                        description += "<p>" + innerText + "</p>" + Environment.NewLine;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return description;
        }

        private static int CharCount(string inputText, string charList)
        {
            return CharCount(inputText, charList.ToCharArray());
        }

        private static int CharCount(string inputText, char[] charList)
        {
            int count = 0;

            foreach (char c in charList)
            {
                count += CharCount(inputText, c);
            }

            return count;
        }

        private static int CharCount(string inputText, char charToCount)
        {
            int count = 0;

            foreach (char c in inputText.ToCharArray())
            {
                if (c.CompareTo(charToCount) == 0)
                    count++;
            }

            return count;
        }

        public static string RemoveWhitespaceWithSplit(string inputText)
        {
            var sb = new StringBuilder();

            string[] parts = inputText.Split(new char[] { ' ', '\n', '\t', '\r', '\f', '\v' }, StringSplitOptions.RemoveEmptyEntries);

            int size = parts.Length;
            for (int i = 0; i < size; i++)
                sb.AppendFormat("{0} ", parts[i]);

            return sb.ToString();
        }

        public static string stripCrLf(string text, string replacementString = "")
        {
            string pattern = @"[\n\r]";
            Regex re = new Regex(pattern, RegexOptions.IgnoreCase);

            return re.Replace(text, replacementString);
        }

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
    }
}
