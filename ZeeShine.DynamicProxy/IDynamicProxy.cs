using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.DynamicProxy
{
    /// <summary>
    /// 动态代理接口
    /// </summary>
    [ProxyIgnore]
    public interface IDynamicProxy
    {
        /// <summary>
        /// 获取代理对象
        /// </summary>
        /// <returns></returns>
        object GetProxy();
    }
}
