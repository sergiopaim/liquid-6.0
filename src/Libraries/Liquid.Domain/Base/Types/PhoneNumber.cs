using System.Text.RegularExpressions;

namespace Liquid.Domain
{
    /// <summary>
    /// Helper class to deal with phone numbers
    /// </summary>
    public static class PhoneNumber
    {
        /// <summary>
        /// Check if a string is a valid phone number
        /// </summary>
        /// <param name="phoneNumber">The string to check</param>
        /// <returns>True if the phoneNumber is a valid one</returns>
        public static bool IsValid(string phoneNumber)
        {
            return Regex.Match(phoneNumber, @"\+[\d\-\*]*\s\([\d]{2,}\)\s[\d]{4,5}\-[\d]{4}").Success;
        }

        /// <summary>
        /// Check if a string is either null or a valid phone number
        /// </summary>
        /// <param name="phoneNumber">The string to check</param>
        /// <returns>True if the phoneNumber is null or a valid phone number</returns>
        public static bool IsNullOrValid(string phoneNumber)
        {
            return phoneNumber is null || IsValid(phoneNumber);
        }

        /// <summary>
        /// Check if a string is either null, string.empty or a valid phone number
        /// </summary>
        /// <param name="phoneNumber">The string to check</param>
        /// <returns>True if the phoneNumber is null, string.empty or a valid phone number</returns>
        public static bool IsNullOrEmptyOrValid(string phoneNumber)
        {
            return string.IsNullOrEmpty(phoneNumber) || IsValid(phoneNumber);
        }
    }
}
