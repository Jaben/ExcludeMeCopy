/* The MIT License (MIT)
 * 
 * Copyright (c) 2013 Jaben Cargman
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace ExcludeMeCopy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class StringExtensions
    {
        public static bool Matches(this string str, IEnumerable<string> wildcards)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            if (wildcards == null)
            {
                throw new ArgumentNullException("wildcards");
            }

            return wildcards
                .Where(x => IsSet(x))
                .Select(x => x.Trim())
                .Any(str.Matches);
        }

        public static bool IsSet(this string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Compares the string against a given pattern.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="wildcard">The wildcard, where "*" means any sequence of characters, and "?" means any single character.</param>
        /// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
        public static bool Matches(this string str, string wildcard)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            if (wildcard == null)
            {
                throw new ArgumentNullException("wildcard");
            }

            var glob = string.Format("^{0}$", Regex.Escape(wildcard).Replace(@"\*", @".*").Replace(@"\?", @"."));

            return new Regex(glob, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(str);
        }
    }
}