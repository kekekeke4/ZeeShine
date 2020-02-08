using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.Utils
{
    public static class ConvertUtils
    {
        public static int ToInt(string input, int defaultValue = 0)
        {
            var result = defaultValue;
            if (!string.IsNullOrEmpty(input))
            {
                if (!int.TryParse(input, out result))
                {
                    result = defaultValue;
                }
            }
            return result;
        }

        public static int ToInt(object input, int defaultValue = 0)
        {
            return ToInt(input?.ToString() ?? string.Empty, defaultValue);
        }

        public static object ToValue(Type type,string input)
        {
            if (type == typeof(String))
            {
                return input;
            }
            else if (type == typeof(bool))
            {
                return Convert.ToBoolean(input);
            }
            else if (type == typeof(Byte))
            {
                return Convert.ToByte(input);
            }
            else if (type == typeof(Char))
            {
                return Convert.ToChar(input);
            }
            else if (type == typeof(DateTime))
            {
                return Convert.ToDateTime(input);
            }
            else if (type == typeof(Decimal))
            {
                return Convert.ToDecimal(input);
            }
            else if (type == typeof(Double))
            {
                return Convert.ToDouble(input);
            }
            else if (type == typeof(Int16))
            {
                return Convert.ToInt16(input);
            }
            else if (type == typeof(Int32))
            {
                return Convert.ToInt32(input);
            }
            else if (type == typeof(Int64))
            {
                return Convert.ToInt64(input);
            }
            else if (type == typeof(Single))
            {
                return Convert.ToSingle(input);
            }
            else
            {
                return input;
            }
        }
    }
}
