using System;

namespace ZeeShine.DynamicProxy
{
    public class ProxyFactory : AdvisedSupport
	{
		public ProxyFactory()
		{
		}

		public ProxyFactory(object target) : 
            base(target)
		{
		}

	    public ProxyFactory(Type[] interfaces) : 
            base(interfaces)
	    {
	    }

	    public virtual object GetProxy()
	    {
	        IDynamicProxy proxy = CreateAopProxy();
	        return proxy.GetProxy();
	    }

        public static object GetProxy(Type interfaceType, IInterceptor interceptor)
        {
            var proxyFactory = new ProxyFactory();
            proxyFactory.Interceptors.Add(interceptor);
            proxyFactory.Interfaces.Add(interfaceType);
            return proxyFactory.GetProxy();
        }
    }
}