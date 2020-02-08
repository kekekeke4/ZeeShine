using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;

namespace ZeeShine.DynamicProxy
{
    [Serializable]
    public class CachedDynamicProxyFactory : IDynamicProxyFactory
    {
        private static readonly ConcurrentDictionary<ProxyTypeCacheKey, Type> typeCache;

        static CachedDynamicProxyFactory()
        {
            typeCache = new ConcurrentDictionary<ProxyTypeCacheKey, Type>();
        }

        /// <summary>
        /// 获取缓存类型个数
        /// </summary>
        public static int CountCachedTypes
        {
            get { return typeCache.Count; }
        }

        /// <summary>
        /// 清空类型缓存
        /// </summary>
        public static void ClearCache()
        {
            typeCache.Clear();
        }

        public CachedDynamicProxyFactory()
        {}

        protected virtual Type BuildProxyType(IProxyTypeBuilder typeBuilder)
        {
            ProxyTypeCacheKey cacheKey = new ProxyTypeCacheKey(typeBuilder.BaseType, typeBuilder.TargetType, typeBuilder.Interfaces, typeBuilder.ProxyTargetAttributes);
            if (!typeCache.TryGetValue(cacheKey, out Type proxyType))
            {
                proxyType = typeBuilder.BuildProxyType();
                typeCache.AddOrUpdate(cacheKey, proxyType, (k, old) => proxyType);
                //typeCache.TryAdd(cacheKey, proxyType);
            }
            return proxyType;
        }

        public IDynamicProxy CreateDynamicProxy(AdvisedSupport advisedSupport)
        {
            if (advisedSupport.ProxyType == null)
            {
                IProxyTypeBuilder typeBuilder;
                if ((advisedSupport.ProxyTargetType) ||
                    (advisedSupport.Interfaces.Count == 0))
                {
                    typeBuilder = new DecoratorProxyTypeBuilder(advisedSupport);
                }
                else
                {
                    typeBuilder = new CompositionProxyTypeBuilder(advisedSupport);
                }
                advisedSupport.ProxyType = BuildProxyType(typeBuilder);
                advisedSupport.ProxyConstructor = advisedSupport.ProxyType.GetConstructor(new Type[] { typeof(IAdvised) });
            }
            return (IDynamicProxy)advisedSupport.ProxyConstructor.Invoke(new object[] { advisedSupport });
        }


        private sealed class ProxyTypeCacheKey
        {
            private sealed class HashCodeComparer : IComparer<Type>
            {
                public int Compare(Type x, Type y)
                {
                    return x.GetHashCode().CompareTo(y.GetHashCode());
                }
            }

            private static HashCodeComparer interfaceComparer = new HashCodeComparer();

            private Type baseType;
            private Type targetType;
            private List<Type> interfaceTypes;
            private bool proxyTargetAttributes;

            public ProxyTypeCacheKey(Type baseType, Type targetType, IList<Type> interfaceTypes, bool proxyTargetAttributes)
            {
                this.baseType = baseType;
                this.targetType = targetType;
                this.interfaceTypes = new List<Type>(interfaceTypes);
                this.interfaceTypes.Sort(interfaceComparer); // sort by GetHashcode()? to have a defined order
                this.proxyTargetAttributes = proxyTargetAttributes;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }
                ProxyTypeCacheKey proxyTypeCacheKey = obj as ProxyTypeCacheKey;
                if (proxyTypeCacheKey == null)
                {
                    return false;
                }
                if (!Equals(targetType, proxyTypeCacheKey.targetType))
                {
                    return false;
                }
                if (!Equals(baseType, proxyTypeCacheKey.baseType))
                {
                    return false;
                }
                for (int i = 0; i < interfaceTypes.Count; i++)
                {
                    if (!Equals(interfaceTypes[i], proxyTypeCacheKey.interfaceTypes[i]))
                    {
                        return false;
                    }
                }
                if (proxyTargetAttributes != proxyTypeCacheKey.proxyTargetAttributes)
                {
                    return false;
                }
                return true;
            }

            public override int GetHashCode()
            {
                int result = baseType.GetHashCode();
                result = 29*result + targetType.GetHashCode();
                for (int i = 0; i < interfaceTypes.Count; i++)
                {
                    result = 29 * result + interfaceTypes[i].GetHashCode();
                }
                result = 29 * result + proxyTargetAttributes.GetHashCode();
                return result;
            }

            public override string ToString()
            {
                StringBuilder buffer = new StringBuilder();
                buffer.Append("baseType=" + baseType + "; ");
                buffer.Append("targetType=" + targetType + "; ");
                buffer.Append("interfaceTypes=[");
                foreach (Type intf in interfaceTypes)
                {
                    buffer.Append(intf + ";");
                }
                buffer.Append("]; ");
                buffer.Append("proxyTargetAttributes=" + proxyTargetAttributes + "; ");
                return buffer.ToString();
            }
        }
    }
}