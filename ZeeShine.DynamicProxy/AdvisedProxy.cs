using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ZeeShine.DynamicProxy
{
    public  class AdvisedProxy : IDynamicProxy
    {
        public IAdvised advised;
        public Type targetType;

        public AdvisedProxy()
        {
        }

        public AdvisedProxy(IAdvised advised) : 
            this()
        {
            this.advised = advised;
        }

        public object Invoke(object proxy, object target, Type targetType,
            MethodInfo targetMethod, MethodInfo proxyMethod, object[] args, IList interceptors)
        {
            List<IInterceptor> tmps = new List<IInterceptor>(interceptors?.Count ?? 0);
            foreach (object obj in interceptors)
            {
                if (obj is IInterceptor)
                {
                    tmps.Add((IInterceptor)obj);
                }
            }

            IInvocation invocation = new DynamicMethodInvocation(proxy, target, targetMethod, proxyMethod, args, targetType, tmps);
            return invocation.Proceed();
        }

        public object GetProxy()
        {
            return this;
        }

        public object GetTarget()
        {
            return advised.Target;
        }

        public IList<object> GetInterceptors(Type targetType, MethodInfo method)
        {
            IList<object> interceptors = new List<object>();
            foreach(IInterceptor interceptor in advised.Interceptors)
            {
                interceptors.Add(interceptor);
            }
            return interceptors;
        }

        public void ReleaseTarget(object target)
        {
            if (target is IDisposable)
            {
                ((IDisposable)target).Dispose();
            }
        }

        public bool IsInterfaceProxied(Type intf)
        {
            return false;
        }
    }
}
