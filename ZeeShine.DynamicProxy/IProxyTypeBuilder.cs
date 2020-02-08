using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ZeeShine.DynamicProxy
{
	public interface IProxyTypeBuilder
	{
        Type BuildProxyType();

		string Name { get; set; }

		Type TargetType { get; set; }

		Type BaseType { get; set; }

        IList<Type> Interfaces { get; set; }

        bool ProxyTargetAttributes { get; set; }

        IList TypeAttributes { get; set; }
		
		IDictionary MemberAttributes { get; set; }

        void PushProxy(ILGenerator il);

        void PushTarget(ILGenerator il);

        void PushAdvisedProxy(ILGenerator il);
    }
}