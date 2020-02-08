using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.DynamicProxy
{
    public interface IDynamicProxyFactory
    {
        IDynamicProxy CreateDynamicProxy(AdvisedSupport advisedSupport);
    }
}
