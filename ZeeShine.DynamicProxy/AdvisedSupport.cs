using System;
using System.Collections.Generic;
using System.Reflection;
using ZeeShine.Utils;

namespace ZeeShine.DynamicProxy
{
    public class AdvisedSupport : IAdvised
    {
        private object target;
        private IDynamicProxyFactory aopProxyFactory;

        public AdvisedSupport()
        {
            Interceptors = new List<IInterceptor>();
            Interfaces = new List<Type>();
            aopProxyFactory = new CachedDynamicProxyFactory();
        }

        public AdvisedSupport(IEnumerable<Type> interfaces) : this()
        {
            if (interfaces != null)
            {
                foreach (var @interface in interfaces)
                {
                    Interfaces.Add(@interface);
                }
            }
        }

        public AdvisedSupport(object target) : 
            this(GetInterfaces(target))
        {
            Target = target;
        }

        protected static Type[] GetInterfaces(object target)
        {
            if (target == null)
            {
                throw new Exception("Can't proxy null object");
            }
            return ReflectionUtils.GetInterfaces(target is Type ? (Type)target : target.GetType());
        }

        public bool ExposeProxy => false;

        public bool ProxyTargetType => true;

        public bool ProxyTargetAttributes => true;

        public IList<IInterceptor> Interceptors
        {
            get;
            private set;
        }

        public IList<Type> Interfaces
        {
            get;
            private set;
        }

        public bool IsFrozen => false;

        public Type TargetType
        {
            get
            {
                return Target?.GetType() ?? typeof(object);
            }
        }

        public object Target
        {
            get
            {
                if (target == null)
                {
                    target = new object();
                }
                return target;
            }
            private set
            {
                target = value;
            }
        }

        public bool IsInterfaceProxied(Type intf)
        {
            if (intf != null)
            {
                foreach (Type proxyInterface in Interfaces)
                {
                    if (intf.IsAssignableFrom(proxyInterface))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取或设置代理类型
        /// </summary>
        internal Type ProxyType
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置代理的构造函数
        /// </summary>
        internal ConstructorInfo ProxyConstructor
        {
            get;
            set;
        }

        /// <summary> 
        /// 创建动态代理实例
        /// </summary>
        protected internal virtual IDynamicProxy CreateAopProxy()
        {
            return aopProxyFactory.CreateDynamicProxy(this);
        }

    }
}