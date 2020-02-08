using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.Utils
{
    public static class JsonUtils
    {
        public static string Serialize(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static T Deserialize<T>(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch
                {
                    return default(T);
                }
            }
            return default(T);
        }

        public static object Deserialize(string json,Type type)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject(json, type);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }
}
