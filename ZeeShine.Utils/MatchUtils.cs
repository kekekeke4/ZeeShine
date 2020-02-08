using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.Utils
{
    public sealed class MatchUtils
    {
        public static bool SimpleMatch(string pattern, string str)
        {
            if ((pattern == str || (pattern != null && pattern.Equals(str))) || "*".Equals(pattern))
            {
                return true;
            }
            if (pattern == null || str == null)
            {
                return false;
            }
            if (pattern.StartsWith("*") && pattern.EndsWith("*") &&
                str.IndexOf(pattern.Substring(1, (pattern.Length - 1) - (1))) != -1)
            {
                return true;
            }
            if (pattern.StartsWith("*") && str.EndsWith(pattern.Substring(1, (pattern.Length) - (1))))
            {
                return true;
            }
            if (pattern.EndsWith("*") && str.StartsWith(pattern.Substring(0, (pattern.Length - 1) - (0))))
            {
                return true;
            }
            return false;
        }

        public static bool SimpleMatch(string[] patterns, string str)
        {
            if (patterns != null)
            {
                for (int i = 0; i < patterns.Length; i++)
                {

                    if (SimpleMatch(patterns[i], str))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
