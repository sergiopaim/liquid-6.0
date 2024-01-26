using System;

namespace Liquid.Domain
{
    /// <summary>
    /// Helper class to deal with endpoint addresses
    /// </summary>
    public static class EndPointAddress
    {
        /// <summary>
        /// Check if a string is a valid endpoint address
        /// </summary>
        /// <param name="url">The url string to check</param>
        /// <returns>True if the url is a valid endpoint address</returns>
        public static bool IsValid(string url)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttps);

            return result;
        }

        /// <summary>
        /// Check if a string is either null or a valid endpoint address
        /// </summary>
        /// <param name="url">The url string to check</param>
        /// <returns>True if the url is null or a valid endpoint address</returns>
        public static bool IsNullOrValid(string url)
        {
            return url is null || IsValid(url);
        }

        /// <summary>
        /// Check if a string is either null, string.empty or a valid endpoint address
        /// </summary>
        /// <param name="url">The url string to check</param>
        /// <returns>True if the url is null, string.empty or a valid endpoint address</returns>
        public static bool IsNullOrEmptyOrValid(string url)
        {
            return string.IsNullOrEmpty(url) || IsValid(url);
        }
    }
}
