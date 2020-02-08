using System;

namespace ZeeShine.DynamicProxy
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    [Serializable]
    public sealed class ProxyIgnoreAttribute : Attribute
    {
        public ProxyIgnoreAttribute()
        {
        }
    }
}