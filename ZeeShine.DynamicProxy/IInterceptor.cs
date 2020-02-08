using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.DynamicProxy
{
    /// <summary>
    /// 拦截器
    /// </summary>
    public interface IInterceptor
    {
        object Invoke(IInvocation invocation);
    }
}
