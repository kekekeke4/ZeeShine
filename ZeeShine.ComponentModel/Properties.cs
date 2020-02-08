using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace ZeeShine.ComponentModel
{
    public class Properties
    {
        private readonly IDictionary<string, string> dics;

        public Properties()
        {
            dics = new Dictionary<string, string>();
        }

        public string this[string name]
        {
            get
            {
                if (dics.ContainsKey(name))
                {
                    return dics[name];
                }
                return string.Empty;
            }
            set
            {
                dics[name] = value;
            }
        }

        public static Properties Read(XmlReader reader)
        {
            var attributes = new Properties();
            if (reader.HasAttributes)
            {
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    reader.MoveToAttribute(i);
                    attributes[reader.Name] = reader.Value;
                }
            }
            return attributes;
        }
    }
}
