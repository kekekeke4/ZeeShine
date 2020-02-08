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
        /// ��ȡ�������Ŀ���������
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// ��ȡ�������Ŀ�����
        /// </summary>
        object Target { get; }

        /// <summary>
        /// �Ƿ�ӿڱ�����
        /// </summary>
        /// <param name="intf"></param>
        /// <returns></returns>
        bool IsInterfaceProxied(Type intf);
    }
}