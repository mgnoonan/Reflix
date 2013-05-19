using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace Reflix.Models
{
    /// <summary>
    /// A Post object represents a single RSS post read from XML data
    /// </summary>
    public class Post
    {
        public string Title { get; private set; }
        public DateTime? Date { get; private set; }
        public string Url { get; private set; }
        public string ImageUrl { get; private set; }
        public string Description { get; private set; }
        public string Creator { get; private set; }
        public string Content { get; private set; }
        public string Link { get; private set; }
        public string Guid { get; private set; }

        private static string GetElementValue(XContainer element, string name)
        {
            if ((element == null) || (element.Element(name) == null))
                return String.Empty;

            return element.Element(name).Value;
        }

        public Post(XContainer post)
        {
            // Get the string properties from the post's element values
            Title = GetElementValue(post, "title");
            Url = GetElementValue(post, "guid");
            Link = GetElementValue(post, "link");
            Guid = GetElementValue(post, "guid");
            Creator = GetElementValue(post, "creator");
            Content = GetElementValue(post, "encoded");

            if (string.IsNullOrWhiteSpace(Url))
                Url = Link;

            string fullDescription = GetElementValue(post, "description");
            Description = fullDescription.Substring(fullDescription.LastIndexOf(">") + 1);

            int start = fullDescription.LastIndexOf("http://");
            int end = fullDescription.LastIndexOf("\"/>");
            int len = end - start;
            if (len > 0)
            {
                string imageUrl = fullDescription.Substring(start, len);
                //string imageFile = imageUrl.Substring(imageUrl.LastIndexOf("/"));
                //string lastFour = imageFile.Substring(imageFile.Length - 8, 4);

                ImageUrl = imageUrl.Replace("/small/", "/large/");
            }

            // The Date property is a nullable DateTime? -- if the pubDate element
            // can't be parsed into a valid date, the Date property is set to null
            DateTime result;
            if (DateTime.TryParse(GetElementValue(post, "pubDate"), out result))
                Date = (DateTime?)result;
        }

        public override string ToString()
        {
            return String.Format("{0} by {1}", Title ?? "no title", Creator ?? "Unknown");
        }
    }
}