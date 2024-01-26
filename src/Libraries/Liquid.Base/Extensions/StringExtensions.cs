using Diacritics.Extensions;
using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Liquid.Base
{    /// <summary>
     /// Implement Extensions for string objects
     /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns the string value as escaped string
        /// </summary>
        /// <param name="value">A string value</param>
        /// <returns>Escaped string value</returns>
        public static string ToEscapedString(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            StringBuilder sb = new(value.Length * value.Length);

            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '\'':
                        sb.Append(@"\'");

                        break;
                    case '\"':
                        sb.Append("\\\"");

                        break;
                    case '\t':
                        sb.Append(@"\t");

                        break;
                    case '\n':
                        sb.Append(@"\n");

                        break;
                    case '\b':
                        sb.Append(@"\b");

                        break;
                    case '\r':
                        sb.Append(@"\r");

                        break;
                    case '\\':
                        sb.Append(@"\\");

                        break;
                    default:
                        sb.Append(ch);

                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes the accents (diacritics) from the string value
        /// </summary>
        /// <param name="value">A string value</param>
        /// <returns>String without accents (diacritics)</returns>
        public static string RemoveAccents(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.RemoveDiacritics();
        }

        /// <summary>
        /// Returns if the string value has accents (diacritics)
        /// </summary>
        /// <param name="value">A string value</param>
        /// <returns>True if the string has accents (diacritics)</returns>
        public static bool HasAccents(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.HasDiacritics();
        }

        /// <summary>
        /// Returns a new string with the first occurrence of oldValue replaced by newValue
        /// </summary>
        /// <param name="text">The string containing the value to be replaced</param>
        /// <param name="oldValue">The string to be replaced</param>
        /// <param name="newValue">The string to replace the first occurrence of oldValue</param>
        /// <returns>The new string with the replaced value</returns>
        public static string ReplaceFirst(this string text, string oldValue, string newValue)
        {
            int pos = text.IndexOf(oldValue);
            if (pos < 0)
                return text;

            return string.Concat(text.AsSpan(0, pos), newValue, text.AsSpan(pos + oldValue.Length));
        }

        /// <summary>
        /// Checks if a string is a valid Guid
        /// </summary>
        /// <param name="value">A string value</param>
        /// <returns>True if the string is a valid Guid</returns>
        public static bool IsGuid(this string value)
        {
            if (value is null)
                return false;

            try
            {
                Guid guid = new(value);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Converts a string into camelCase form
        /// </summary>
        /// <param name="text">The string to be converted</param>
        /// <returns></returns>
        public static string ToCamelCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string[] words = text.Split(new char[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);

            string result = words[0].ToLower();
            for (int i = 1; i < words.Length; i++)
                result += string.Concat(words[i][..1].ToUpper(), words[i].AsSpan(1));

            return result;
        }

        /// <summary>
        /// Converts a string into PascalCase form
        /// </summary>
        /// <param name="text">The string to be converted</param>
        /// <returns></returns>
        public static string ToPascalCase(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = text.ToCamelCase();

            return text[..1].ToUpper() + text[1..];
        }

        /// <summary>
        /// Converts a string into 'Title Case' form
        /// </summary>
        /// <param name="text">The string to be converted</param>
        /// <returns></returns>
        public static string ToTitleCase(this string text)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            TextInfo textInfo = culture.TextInfo;
            string[] words = text.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                if (ShouldCapitalize(words[i], i, culture))
                {
                    words[i] = textInfo.ToTitleCase(words[i]);
                }
                else
                {
                    words[i] = words[i].ToLower(culture);
                }
            }

            return string.Join(" ", words);
        }

        static bool ShouldCapitalize(string word, int currentIndex, CultureInfo culture)
        {
            string[] excludedWordsEnglish = { "a", "an", "and", "as", "at", "but", "by", "for", "from", "in", "nor", "of", "on", "or", "over", "the", "to", "under", "up", "with" };
            string[] excludedWordsPortuguese = { "a", "as", "com", "da", "das", "de", "do", "dos", "e", "em", "na", "nas", "no", "nos", "o", "os", "para", "por", "um", "uma", "umas", "uns" };

            string[] excludedWords;
            if (culture.TwoLetterISOLanguageName == "pt")
            {
                excludedWords = excludedWordsPortuguese;
            }
            else
            {
                excludedWords = excludedWordsEnglish;
            }

            if (currentIndex == 0)
            {
                return true;
            }

            return Array.IndexOf(excludedWords, word.ToLower(culture)) == -1;
        }
    }
}