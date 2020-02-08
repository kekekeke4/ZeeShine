using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ZeeShine.DynamicProxy
{
    public class DynamicMethodInvocation : IInvocation
    {
        private int currentInterceptorIndex;

        public DynamicMethodInvocation(object proxy,
            object target,
            MethodInfo targetMethod,
            MethodInfo proxyMethod,
            object[] arguments,
            Type targetType,
            IList<IInterceptor> interceptors)
        {
            Proxy = proxy;
            Target = target;
            TargetMethod = targetMethod;
            ProxyMethod = proxyMethod;
            Arguments = arguments;
            TargetType = targetType;
            Interceptors = interceptors;
        }

        public object[] Arguments
        {
            get;
            private set;
        }

        public object Target
        {
            get;
            private set;
        }

        public MethodInfo ProxyMethod
        {
            get;
            private set;
        }

        public MethodInfo TargetMethod
        {
            get;
            private set;
        }

        public object Proxy
        {
            get;
            private set;
        }

        public Type TargetType
        {
            get;
            private set;
        }

        public object Proceed()
        {
            if (Interceptors == null ||
                Interceptors.Count == currentInterceptorIndex)
            {
                var invokeMethodInfo = ProxyMethod ?? TargetMethod;
                return invokeMethodInfo.Invoke(Target, Arguments);
            }

            var interceptor = Interceptors[currentInterceptorIndex];
            var invocation = PrepareInvocationForProceed(this); // next
            return interceptor.Invoke(invocation);
        }

        internal IList<IInterceptor> Interceptors
        {
            get;
            private set;
        }

        private IInvocation PrepareInvocationForProceed(IInvocation invocation)
        {
            var rmi = new DynamicMethodInvocation(Proxy, Target, TargetMethod, ProxyMethod, Arguments, TargetType, Interceptors);
            rmi.currentInterceptorIndex = currentInterceptorIndex + 1;
            return rmi;
        }
    }
}
