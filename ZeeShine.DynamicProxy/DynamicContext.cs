using System;
using System.Collections;

namespace ZeeShine.DynamicProxy
{
	public sealed class DynamicContext
	{
        [ThreadStatic]
	    private static Stack tls_ProxyStack;

		private static Stack ProxyStack
        {
            get 
            {
                if (tls_ProxyStack == null)
                {
                    tls_ProxyStack = new Stack();
                }
                return tls_ProxyStack;
            }
        }

        public static bool IsActive
	    {
	        get
	        {
	            return (tls_ProxyStack != null && tls_ProxyStack.Count > 0);	            
	        }
	    }

		public static object CurrentProxy
		{
			get
			{
			    Stack proxyStack = ProxyStack;
                if (proxyStack.Count == 0)
				{
					throw new Exception(
						"Cannot find proxy: Set the 'ExposeProxy' property " +
						"to 'true' on IAdvised to make it available.");
				}
                return proxyStack.Peek();
			}
		}

		public static void PushProxy(object proxy)
		{
            ProxyStack.Push(proxy);
		}

		public static void PopProxy()
		{
		    Stack proxyStack = ProxyStack;
		    if (proxyStack.Count == 0)
			{
				throw new Exception(
					"Proxy stack empty. Always call 'PushProxy' before 'PopProxy'.");
			}
            proxyStack.Pop();
		}

		private DynamicContext()
		{
		}
	}
}