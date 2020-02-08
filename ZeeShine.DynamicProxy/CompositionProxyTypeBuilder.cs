
using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace ZeeShine.DynamicProxy
{
	/// <summary>
    /// 组合模式代理构建器
	/// </summary>
	public class CompositionProxyTypeBuilder : AbstractProxyTypeBuilder
    {
        private const string PROXY_TYPE_NAME = "CompositionDynamicProxy";

        private readonly IAdvised advised;
        
        public CompositionProxyTypeBuilder(IAdvised advised)
        {
            this.advised = advised;

            Name = PROXY_TYPE_NAME;
            BaseType = typeof(AdvisedProxy);
            TargetType = advised.TargetType.IsInterface ? typeof(object) : advised.TargetType;
            Interfaces = GetProxiableInterfaces(advised.Interfaces);
            ProxyTargetAttributes = advised.ProxyTargetAttributes;
        }

        /// <summary>
        /// 创建代理类型
        /// </summary>
        public override Type BuildProxyType()
		{
            IDictionary targetMethods = new Hashtable();

            TypeBuilder typeBuilder = CreateTypeBuilder(Name, BaseType);

            // 为代理类型应用自定义特性
            ApplyTypeAttributes(typeBuilder, TargetType);

            //if (advised.IsSerializable)
            //{
            //    typeBuilder.SetCustomAttribute(
            //        ReflectionUtils.CreateCustomAttribute(typeof(SerializableAttribute)));
            //    ImplementSerializationConstructor(typeBuilder);
            //}

            // 创建构造函数
			ImplementConstructors(typeBuilder);

			// 实现接口
            //IDictionary interfaceMap = advised.InterfaceMap;
			foreach (Type intf in Interfaces)
			{
				//object target = interfaceMap[intf];
				//if (target == null)
				//{
    //                // implement interface
					ImplementInterface(typeBuilder, 
                        new TargetProxyMethodBuilder(typeBuilder, this, false, targetMethods), 
                        intf, TargetType);
				//}
				//else if (target is IIntroductionAdvisor)
				//{
    //                // implement introduction
				//	ImplementInterface(typeBuilder,
    //                    new IntroductionProxyMethodBuilder(typeBuilder, this, targetMethods, advised.IndexOf((IIntroductionAdvisor) target)),
    //                    intf, TargetType);
				//}
			}

            //Type proxyType;
            //proxyType = typeBuilder.CreateType();
            Type proxyType = typeBuilder.CreateTypeInfo().AsType();

            // 设置目标方法引用
            foreach (DictionaryEntry entry in targetMethods)
			{
				FieldInfo field = proxyType.GetField((string) entry.Key, BindingFlags.NonPublic | BindingFlags.Static);
				field.SetValue(proxyType, entry.Value);
			}

			return proxyType;
		}

        public override void PushAdvisedProxy(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_0);
        }

        ///// <summary>
        ///// 实现序列化构造器
        ///// </summary>
        ///// <param name="typeBuilder"></param>
        //private void ImplementSerializationConstructor(TypeBuilder typeBuilder)
        //{
        //    ConstructorBuilder cb =
        //        typeBuilder.DefineConstructor(MethodAttributes.Family,
        //                                      CallingConventions.Standard,
        //                                      new Type[] { typeof(SerializationInfo), typeof(StreamingContext) });

        //    ILGenerator il = cb.GetILGenerator();
        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Ldarg_1);
        //    il.Emit(OpCodes.Ldarg_2);
        //    il.Emit(OpCodes.Call, References.BaseCompositionAopProxySerializationConstructor);
        //    il.Emit(OpCodes.Ret);
        //}

        /// <summary>
        /// 实现代理类的构造器
        /// </summary>
        /// <param name="typeBuilder">
        /// </param>
        protected override void ImplementConstructors(TypeBuilder typeBuilder)
        {
            ConstructorBuilder cb =
                typeBuilder.DefineConstructor(References.AdvisedProxyConstructor.Attributes,
                                              References.AdvisedProxyConstructor.CallingConvention,
                                              new Type[] { typeof(IAdvised) });

            ILGenerator il = cb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, References.AdvisedProxyConstructor);
            il.Emit(OpCodes.Ret);
        }

        public static bool IsCompositionProxy(Type type)
        {
            return type.FullName.StartsWith(PROXY_TYPE_NAME);
        }
	}
}