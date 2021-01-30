using System.Text.RegularExpressions;

namespace Reflix.Helpers
{
    /// <summary>
    /// Helper class containing .NET extenstion methods
    /// </summary>
    public static class Extensions
    {
        public static string Left(this string s, int length)
        {
            if (length > s.Length)
                return s;

            return s.Substring(0, length);
        }

        public static string Right(this string s, int length)
        {
            if (length > s.Length)
                return s;

            return s[^length..];
        }

        public static string StripAfter(this string s, string searchValue)
        {
            int pos = s.IndexOf(searchValue);
            if (pos == -1)
                return s;

            return s.Substring(0, pos);
        }

        public static bool StartsWithAlphaNumeric(this string s)
        {
            char firstChar = s.ToLowerInvariant().ToCharArray(0, 1)[0];

            return (firstChar >= 'a' && firstChar <= 'z');
        }

        public static bool IsNumeric(this string s)
        {
            Regex pattern = new Regex("[^0-9]");
            return !pattern.IsMatch(s);
        }

        /// <summary>
        /// Removes all HTML markup from a string
        /// </summary>
        /// <param name="s">The string containing HTML source</param>
        /// <returns>The text without any markup</returns>
        public static string RemoveMarkup(this string s)
        {
            return Regex.Replace(s, "<[^>]*>", " ");
        }
    }
}
