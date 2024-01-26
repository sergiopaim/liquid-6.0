using Liquid.Interfaces;
using System;
using System.Globalization;
using System.Threading;
using TimeZoneConverter;

namespace Liquid.Platform
{
    /// <summary>
    /// Helper class to format strings, numbers, dates and times according to user's culture (language and timezone)
    /// </summary>
    public class FormatterByProfile
    {
        const string DEFAULT_LANGUAGE = "pt"; //Brazilian is the default culture
        const string DEFAULT_TIMEZONE = "America/Sao_Paulo"; //Brazilian is the default culture

        private string callerOldCurrentLanguage;
        private string localOldCurrentLanguage;

        private readonly string userLanguage = DEFAULT_LANGUAGE;
        private readonly string userTimeZone = DEFAULT_TIMEZONE;

        /// <summary>
        /// New instance of FormatterByProfile for a given userId
        /// </summary>
        public FormatterByProfile()
        {
            userLanguage = Thread.CurrentThread.CurrentUICulture.Name;
        }

        /// <summary>
        /// New instance of FormatterByProfile for a given userId
        /// </summary>
        /// <param name="lightDomain">The lightDomain calling class (this)</param>
        /// <param name="userId">The user´s Id</param>
        public FormatterByProfile(ILightDomain lightDomain, string userId)
        {
            var profile = PlatformServices.GetUserProfile(lightDomain, userId);
            if (!string.IsNullOrEmpty(profile?.Language))
                userLanguage = profile.Language;
            if (!string.IsNullOrEmpty(profile?.TimeZone))
                userTimeZone = profile.TimeZone;
        }

        /// <summary>
        /// New instance of FormatterByProfile for a given userId
        /// </summary>
        /// <param name="profile">The user´s profile</param>
        public FormatterByProfile(ProfileBasicVM profile)
        {
            if (!string.IsNullOrEmpty(profile?.Language))
                userLanguage = profile.Language;
            if (!string.IsNullOrEmpty(profile?.TimeZone))
                userTimeZone = profile.TimeZone;
        }

        /// <summary>
        /// New instance of FormatterByProfile for given language and timezone
        /// </summary>
        /// <param name="language">The language to use in format</param>
        /// <param name="timeZone">The timeZone to use in format</param>
        public FormatterByProfile(string language, string timeZone)
        {
            if (!string.IsNullOrEmpty(language))
                userLanguage = language;
            if (!string.IsNullOrEmpty(timeZone))
                userTimeZone = timeZone;
        }

        /// <summary>
        /// Converts a Coordinated Universal Time (UTC) to the time in user profile's time zone
        /// </summary>
        /// <param name="date">The date and time in UTC form</param>
        /// <returns>The date and time in local time</returns>
        public DateTime ToUserLocalTime(DateTime date)
        {
            ChangeLanguage();

            var dateTimeTZ = TimeZoneInfo.ConvertTimeFromUtc(date, TZConvert.GetTimeZoneInfo(userTimeZone));
            RestoreLanguage();

            return dateTimeTZ;
        }

        /// <summary>
        /// Formats a date value as a string based on user's culture (language and timezone)
        /// </summary>
        /// <param name="date">The date value to be formatted</param>
        /// <returns>The date value formatted as string</returns>
        public string FormatDate(DateTime date)
        {
            ChangeLanguage();

            var dateTimeTZ = TimeZoneInfo.ConvertTimeFromUtc(date, TZConvert.GetTimeZoneInfo(userTimeZone));
            var ret = dateTimeTZ.ToString(userLanguage == "en" ? "dddd, MMM dd" : "dddd, dd/MMM",
                                           Thread.CurrentThread.CurrentUICulture.DateTimeFormat);
            RestoreLanguage();

            return ret;
        }

        /// <summary>
        /// Formats a DateTime value as a string based on user's culture (language and timezone)
        /// </summary>
        /// <param name="dateTime">The DateTime value to be formatted</param>
        /// <returns>The DateTime value formatted as string</returns>
        public string FormatDateTime(DateTime dateTime)
        {
            ChangeLanguage();

            var dateTimeTZ = TimeZoneInfo.ConvertTimeFromUtc(dateTime, TZConvert.GetTimeZoneInfo(userTimeZone));
            var culture = Thread.CurrentThread.CurrentUICulture;


            var ret = dateTimeTZ.ToString(culture.DateTimeFormat.FullDateTimePattern, culture.DateTimeFormat);
            RestoreLanguage();
            return ret;
        }

        /// <summary>
        /// Formats a time value as a string based on user's culture (language and timezone)
        /// </summary>
        /// <param name="time">The time value to be formatted</param>
        /// <returns>Time value formatted as string</returns>
        public string FormatTime(DateTime time)
        {
            ChangeLanguage();

            var dateTimeTZ = TimeZoneInfo.ConvertTimeFromUtc(time, TZConvert.GetTimeZoneInfo(userTimeZone));
            var culture = Thread.CurrentThread.CurrentUICulture;

            var ret = dateTimeTZ.ToString(culture.DateTimeFormat.ShortTimePattern, culture);

            RestoreLanguage();
            return ret;
        }

        /// <summary>
        /// Formats a decimal value as a string based on user's culture (language)
        /// </summary>
        /// <param name="value">The decimal value to be formatted</param>
        /// <param name="digits">The number of decimal places to consider (default is 2)</param>
        /// <returns>Time value formatted as string (ex: 999.99 -> R$ 999,99 in PT-BR)</returns>
        public string FormatAsCurrency(decimal value, int? digits = 2)
        {
            return $"{(value < 0 ? "-" : string.Empty)}R$ {FormatAsNumber(Math.Abs(value), digits)}";
        }

        /// <summary>
        /// Formats a currency decimal value as a string based on user's culture (language)
        /// </summary>
        /// <param name="value">The decimal value to be formatted</param>
        /// <param name="digits">The number of decimal places to consider</param>
        /// <returns>Time value formatted as string</returns>
        public string FormatAsNumber(decimal value, int? digits = null)
        {
            ChangeLanguage();

            var culture = Thread.CurrentThread.CurrentUICulture;

            var nfi = culture.NumberFormat;

            if (digits.HasValue)
                nfi.NumberDecimalDigits = digits.Value;

            var ret = value.ToString("N", nfi);
            RestoreLanguage();
            return ret;
        }

        /// <summary>
        /// Formats a percentual decimal value as a string based on user's culture (language)
        /// </summary>
        /// <param name="value">The decimal value to be formatted</param>
        /// <param name="digits">The number of decimal places to consider</param>
        /// <returns>Time value formatted as string</returns>
        public string FormatAsPercent(decimal value, int? digits = 2)
        {
            ChangeLanguage();

            var culture = Thread.CurrentThread.CurrentUICulture;

            var nfi = culture.NumberFormat;

            if (digits.HasValue)
                nfi.PercentDecimalDigits = digits.Value;

            var ret = value.ToString("P", nfi);

            RestoreLanguage();
            return ret;
        }

        /// <summary>
        /// Changes the current language 
        /// </summary>
        /// <param name="language">The new language</param>
        /// <returns>The previous current language</returns>
        public static string SetCurrentLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
                language = DEFAULT_LANGUAGE;

            return ChangeCurrentLanguage(language);
        }

        /// <summary>
        /// Changes current language to the user's culture
        /// </summary>
        public void ApplyUserLanguage()
        {
            callerOldCurrentLanguage = ChangeCurrentLanguage(userLanguage);
        }

        /// <summary>
        /// Restores current language to original (of the caller)
        /// </summary>
        public void RemoveUserLanguage()
        {
            ChangeCurrentLanguage(callerOldCurrentLanguage);
        }

        private void ChangeLanguage()
        {
            localOldCurrentLanguage = ChangeCurrentLanguage(userLanguage);
        }

        private void RestoreLanguage()
        {
            ChangeCurrentLanguage(localOldCurrentLanguage);
        }

        private static string ChangeCurrentLanguage(string culture)
        {
            string oldLanguage = CultureInfo.CurrentCulture?.Name;

            if (!string.IsNullOrEmpty(culture))
            {
                CultureInfo.CurrentCulture = new(culture);
                CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            }
            return oldLanguage;
        }
    }
}
