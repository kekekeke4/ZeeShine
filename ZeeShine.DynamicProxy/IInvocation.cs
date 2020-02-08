using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ZeeShine.DynamicProxy
{
    public interface IInvocation
    {
		object[] Arguments { get; }

        object Target { get; }

        MethodInfo ProxyMethod { get; }

        MethodInfo TargetMethod { get; }

        object Proxy { get; }

        Type TargetType { get; }

        object Proceed();
    }
}
