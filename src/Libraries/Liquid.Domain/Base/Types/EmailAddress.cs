using System.Text.RegularExpressions;

namespace Liquid.Domain
{
    /// <summary>
    /// Helper class to deal with email addresses
    /// </summary>
    public static class EmailAddress
    {
        /// <summary>
        /// Check if a string is a valid email address
        /// </summary>
        /// <param name="emailAddress">The string to check</param>
        /// <returns>True if the emailAddress is a valid one</returns>
        public static bool IsValid(string emailAddress)
        {
            return Regex.Match(emailAddress, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z").Success;
            
        }

        /// <summary>
        /// Check if a string is either null or a valid email address
        /// </summary>
        /// <param name="emailAddress">The string to check</param>
        /// <returns>True if the emailAddress is null or a valid email addressr</returns>
        public static bool IsNullOrValid(string emailAddress)
        {
            return emailAddress is null || IsValid(emailAddress);
        }

        /// <summary>
        /// Check if a string is either null, string.empty or a valid email address
        /// </summary>
        /// <param name="emailAddress">The string to check</param>
        /// <returns>True if the emailAddress is null, string.empty or a valid email addressr</returns>
        public static bool IsNullOrEmptyOrValid(string emailAddress)
        {
            return string.IsNullOrEmpty(emailAddress) || IsValid(emailAddress);
        }
    }
}
