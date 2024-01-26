using System;
using System.Collections.Generic;
using System.Linq;

namespace Liquid.Base
{
    /// <summary>
    /// Base36 encoding/decoding util class
    /// </summary>
    public static class Base36
    {
        private static readonly string BASE_36 = "0123456789abcdefghijklmnopqrstuvwxyz";
        private static readonly char[] CHAR_LIST = BASE_36.ToCharArray();

        /// <summary>
        /// Encode a random unsigned long number into a Base36 string
        /// </summary>
        /// <returns></returns>
        public static string Encode()
        {
            return Encode((ulong)Math.Abs(Random.Shared.NextInt64()));
        }

        /// <summary>
        /// Encode the given number into a Base36 string
        /// </summary>
        /// <param name="input">the long value to encode</param>
        /// <returns></returns>
        public static string Encode(ulong input)
        {
            var result = new Stack<char>();
            while (input != 0)
            {
                result.Push(CHAR_LIST[input % 36]);
                input /= 36;
            }
            return new string(result.ToArray());
        }

        /// <summary>
        /// Decode the Base36 Encoded string into a number
        /// </summary>
        /// <param name="input">the string to decode</param>
        /// <returns></returns>
        public static ulong Decode(string input)
        {
            var reversed = input.ToLower().Reverse();
            ulong result = 0;
            int pos = 0;
            foreach (char c in reversed)
            {
                result += (ulong)(BASE_36.IndexOf(c) * (long)Math.Pow(36, pos));
                pos++;
            }
            return result;
        }
    }
}