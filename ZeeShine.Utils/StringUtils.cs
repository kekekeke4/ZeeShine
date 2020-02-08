using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ZeeShine.Utils
{
    public sealed class StringUtils
    {
        public static readonly string[] EmptyStrings = new string[] { };

        private const string AntExpressionPrefix = "${";

        private const string AntExpressionSuffix = "}";

        private StringUtils()
        {
        }

        public static string[] Split(
            string s, string delimiters, bool trimTokens, bool ignoreEmptyTokens)
        {
            return Split(s, delimiters, trimTokens, ignoreEmptyTokens, null);
        }

        public static string[] Split(
            string s, string delimiters, bool trimTokens, bool ignoreEmptyTokens, string quoteChars)
        {
            if (s == null)
            {
                return new string[0];
            }
            if (string.IsNullOrEmpty(delimiters))
            {
                return new string[] { s };
            }
            if (quoteChars == null)
            {
                quoteChars = string.Empty;
            }
            AssertUtils.IsTrue( quoteChars.Length % 2 == 0, "the number of quote characters must be even" );
            
            char[] delimiterChars = delimiters.ToCharArray();

            // …®√Ë∑÷∏Ó∑˚Œª÷√
            int[] delimiterPositions = new int[s.Length];
            int count = MakeDelimiterPositionList(s, delimiterChars, quoteChars, delimiterPositions);

            List<string> tokens = new List<string>(count+1);
            int startIndex = 0;
            for (int ixSep = 0; ixSep < count; ixSep++)
            {
                string token = s.Substring(startIndex, delimiterPositions[ixSep] - startIndex);
                if (trimTokens)
                {
                    token = token.Trim();
                }
				if (!(ignoreEmptyTokens && token.Length == 0))
				{
					tokens.Add(token);
				}
                startIndex = delimiterPositions[ixSep] + 1;
            }

            if (startIndex < s.Length)
            {
                string token = s.Substring(startIndex);
                if (trimTokens)
                {
                    token = token.Trim();
                }
                if (!(ignoreEmptyTokens && token.Length == 0))
                {
                    tokens.Add(token);
                }
            }
            else if (startIndex == s.Length)
            {
                if (!(ignoreEmptyTokens))
                {
                    tokens.Add(string.Empty);
                }
            }

            return tokens.ToArray();
        }

        private static int MakeDelimiterPositionList(string s, char[] delimiters, string quoteChars, int[] delimiterPositions)
        {
            int count = 0;
            int quoteNestingDepth = 0;
            char expectedQuoteOpenChar = '\0';
            char expectedQuoteCloseChar = '\0';

            for (int ixCurChar = 0; ixCurChar < s.Length; ixCurChar++)
            {
                char curChar = s[ixCurChar];

                for (int ixCurDelim = 0; ixCurDelim < delimiters.Length; ixCurDelim++)
                {
                    if (delimiters[ixCurDelim] == curChar)
                    {
                        if (quoteNestingDepth == 0)
                        {
                            delimiterPositions[count] = ixCurChar;
                            count++;
                            break;
                        }
                    }

                    if (quoteNestingDepth == 0)
                    {
                        for (int ixCurQuoteChar = 0; ixCurQuoteChar < quoteChars.Length; ixCurQuoteChar+=2)
                        {
                            if (quoteChars[ixCurQuoteChar] == curChar)
                            {
                                quoteNestingDepth++;
                                expectedQuoteOpenChar = curChar;
                                expectedQuoteCloseChar = quoteChars[ixCurQuoteChar + 1];
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (curChar == expectedQuoteOpenChar)
                        {
                            quoteNestingDepth++;
                        }
                        else if (curChar == expectedQuoteCloseChar)
                        {
                            quoteNestingDepth--;
                        }
                    }
                }
            }
            return count;
        }

        public static string[] CommaDelimitedListToStringArray(string s)
        {
            return Split(s, ",", false, false, "\"\"");
        }

        public static string[] DelimitedListToStringArray(string input, string delimiter)
        {
            if (input == null)
            {
                return new string[0];
            }

            if (!HasLength(delimiter))
            {
                return new string[] { input };
            }

            return Split(input, delimiter, false, false, null);
        }

        public static string CollectionToDelimitedString<T>(
            IEnumerable<T> c, string delimiter)
        {
            if (c == null)
            {
                return "null";
            }

            var sb = new StringBuilder();
            var i = 0;
            foreach (object obj in c)
            {
                if (i++ > 0)
                {
                    sb.Append(delimiter);
                }
                sb.Append(obj);
            }

            return sb.ToString();
        }

        public static string CollectionToCommaDelimitedString<T>(IEnumerable<T> collection)
        {
            return CollectionToDelimitedString(collection, ",");
        }

        public static string ArrayToCommaDelimitedString<T>(IEnumerable<T> source)
        {
            return ArrayToDelimitedString(source, ",");
        }

        public static string ArrayToDelimitedString<T>(IEnumerable<T> source, string delimiter)
        {
            if (source == null)
            {
                return "null";
            }
            else
            {
                return StringUtils.CollectionToDelimitedString(source, delimiter);
            }
        }

        public static bool HasLength(string target)
        {
            return (target != null && target.Length > 0);
        }

        public static bool HasText(string target)
        {
            if (target == null)
            {
                return false;
            }
            else
            {
                return HasLength(target.Trim());
            }
        }

        public static bool IsNullOrEmpty(string target)
        {
            return !HasText(target);
        }

        public static string GetTextOrNull(string value)
        {
            if (!HasText(value))
            {
                return null;
            }
            return value;
        }

        public static string StripFirstAndLastCharacter(string text)
        {
            if (text != null
                && text.Length > 2)
            {
                return text.Substring(1, text.Length - 2);
            }
            else
            {
                return String.Empty;
            }
        }

        public static IList<string> GetAntExpressions(string text)
        {
            List<string> expressions = new List<string>();
            if (StringUtils.HasText(text))
            {
                int start = text.IndexOf(AntExpressionPrefix);
                while (start >= 0)
                {
                    int end = text.IndexOf(AntExpressionSuffix, start + 2);
                    if (end == -1)
                    {
                        start = -1;
                    }
                    else
                    {
                        string exp = text.Substring(start + 2, end - start - 2);
                        if (StringUtils.IsNullOrEmpty(exp))
                        {
                            throw new FormatException(
                                string.Format("Empty {0}{1} value found in text : '{2}'.",
                                              AntExpressionPrefix,
                                              AntExpressionSuffix,
                                              text));
                        }
                        if (expressions.IndexOf(exp) < 0)
                        {
                            expressions.Add(exp);
                        }
                        start = text.IndexOf(AntExpressionPrefix, end);
                    }
                }
            }
            return expressions;
        }

        public static string SetAntExpression(string text, string expression, object expValue)
        {
            if (StringUtils.IsNullOrEmpty(text))
            {
                return String.Empty;
            }
            if (expValue == null)
            {
                expValue = String.Empty;
            }
            return text.Replace(
                StringUtils.Surround(AntExpressionPrefix, expression, AntExpressionSuffix), expValue.ToString());
        }

        public static string Surround(object fix, object target)
        {
            return StringUtils.Surround(fix, target, fix);
        }

        public static string Surround(object prefix, object target, object suffix)
        {
            return string.Format(
                CultureInfo.InvariantCulture, "{0}{1}{2}", prefix, target, suffix);
        }

        public static string ConvertEscapedCharacters(string inputString)
        {
            if (inputString == null) return null;
            StringBuilder sb = new StringBuilder(inputString.Length);
            for (int i = 0; i < inputString.Length; i++)
            {
                if (inputString[i].Equals('\\'))
                {
                    i++;
                    if (inputString[i].Equals('t'))
                    {
                        sb.Append('\t');
                    }
                    else if (inputString[i].Equals('r'))
                    {
                        sb.Append('\r');
                    }
                    else if (inputString[i].Equals('n'))
                    {
                        sb.Append('\n');
                    }
                    else if (inputString[i].Equals('\\'))
                    {
                        sb.Append('\\');
                    }
                    else
                    {
                        sb.Append("\\" + inputString[i]);
                    }
                }
                else
                {
                    sb.Append(inputString[i]);
                }
            }
            return sb.ToString();
        }
    }
}