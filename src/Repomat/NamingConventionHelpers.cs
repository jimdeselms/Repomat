using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat
{
    /// <summary>
    /// Here's how this works.
    /// 
    /// First, the incoming string needs to be normalized into Pascal case.
    /// Then, we pass the pascal-cased thing into the function.
    /// </summary>
    public static class NamingConventionHelpers
    {
        private readonly static char[] DELIMETERS = {'_'};

        public static string ToLowercaseWords(string s)
        {
            string result = "";
            foreach (char c in s)
            {
                if (Char.IsUpper(c))
                {
                    result += "_";
                }

                result += c;
            }

            return string.Join("_", result.Split(DELIMETERS, StringSplitOptions.RemoveEmptyEntries).Select(part => part.ToLower()));
        }

        public static string ToUppercaseWords(string s)
        {
            return ToLowercaseWords(s).ToUpper();
        }

        public static string ToPascalCase(string s)
        {
            string asWords = ToLowercaseWords(s);
            string[] parts = asWords.Split(DELIMETERS, StringSplitOptions.RemoveEmptyEntries);
            string result = "";
            foreach (string part in parts)
            {
                result += part.Capitalize();
            }

            return result;
        }

        public static string ToCamelCase(string s)
        {
            return ToPascalCase(s).Uncapitalize();
        }
    }
}
