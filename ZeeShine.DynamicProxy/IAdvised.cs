using System;
using System.Collections.Generic;

namespace ZeeShine.DynamicProxy
{
    [ProxyIgnore]
    public interface IAdvised
    {
        bool ExposeProxy { get; }

        bool ProxyTargetType { get; }

        bool ProxyTargetAttributes { get; }

        IList<IInterceptor> Interceptors { get; }

        IList<Type> Interfaces { get; }

        bool IsFrozen { get; }

        /// <summary>
        /// 获取被代理的目标对象类型
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// 获取被代理的目标对象
        /// </summary>
        object Target { get; }

        /// <summary>
        /// 是否接口被代理
        /// </summary>
        /// <param name="intf"></param>
        /// <returns></returns>
        bool IsInterfaceProxied(Type intf);
    }
}