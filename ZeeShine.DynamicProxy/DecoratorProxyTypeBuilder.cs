using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using ZeeShine.Utils;

namespace ZeeShine.DynamicProxy
{
    /// <summary>
    /// 装饰模式构造器
    /// </summary>
    public class DecoratorProxyTypeBuilder : AbstractProxyTypeBuilder
    {
        private const string PROXY_TYPE_NAME = "DecoratorDynamicProxy";

        private IAdvised advised;

        /// <summary>
        /// AdvisedProxy 实例字段
        /// </summary>
        protected FieldBuilder advisedProxyField;

        public DecoratorProxyTypeBuilder(IAdvised advised)
        {
            if (!ReflectionUtils.IsTypeVisible(advised.TargetType, DynamicProxyManager.ASSEMBLY_NAME))
            {
                throw new Exception(String.Format(
                    "Cannot create decorator-based IAopProxy for a non visible class [{0}]",
                    advised.TargetType.FullName));
            }

            if (advised.TargetType.IsSealed)
            {
                throw new Exception(String.Format(
                    "Cannot create decorator-based IAopProxy for a sealed class [{0}]",
                    advised.TargetType.FullName));
            }

            this.advised = advised;
            Name = PROXY_TYPE_NAME;
            TargetType = advised.TargetType.IsInterface ? typeof(object) : advised.TargetType;
            BaseType = TargetType;
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

            // 为代理的类型应用自定义特性
            ApplyTypeAttributes(typeBuilder, TargetType);

            // 声明字段
            DeclareAdvisedProxyInstanceField(typeBuilder);

            //// 实现 ISerializable 接口
            //if (advised.IsSerializable)
            //{
            //    typeBuilder.SetCustomAttribute(
            //        ReflectionUtils.CreateCustomAttribute(typeof(SerializableAttribute)));
            //    ImplementSerializationConstructor(typeBuilder);
            //    ImplementGetObjectDataMethod(typeBuilder);
            //}

            // 床架构造函数
            ImplementConstructors(typeBuilder);

            // 实现接口
            //IDictionary interfaceMap = advised.InterfaceMap;
            foreach (Type intf in Interfaces)
            {
                //object target = interfaceMap[intf];
                //if (target == null)
                //{
                //    // implement interface (proxy only final methods)
                    ImplementInterface(typeBuilder,
                        new TargetProxyMethodBuilder(typeBuilder, this, true, targetMethods),
                        intf, TargetType, false);
                //}
                //else if (target is IIntroductionAdvisor)
                //{
                //    // implement introduction
                //    ImplementInterface(typeBuilder,
                //        new IntroductionProxyMethodBuilder(typeBuilder, this, targetMethods, advised.IndexOf((IIntroductionAdvisor)target)),
                //        intf, TargetType);
                //}
            }

            // 从目标类型进行继承
            InheritType(typeBuilder, 
                new TargetProxyMethodBuilder(typeBuilder, this, false, targetMethods), 
                TargetType);

            //// 实现IAdvised接口
            //ImplementInterface(typeBuilder,
            //    new IAdvisedProxyMethodBuilder(typeBuilder, this),
            //    typeof(IAdvised), TargetType);

            // 实现IAopProxy接口
            ImplementIAopProxy(typeBuilder);

            //Type proxyType;
            //proxyType = typeBuilder.CreateType();
            Type proxyType = typeBuilder.CreateTypeInfo().AsType();

            // 设置目标方法引用
            foreach (DictionaryEntry entry in targetMethods)
            {
                FieldInfo field = proxyType.GetField((string)entry.Key, BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(proxyType, entry.Value);
            }

            return proxyType;
        }

        public override void PushAdvisedProxy(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, advisedProxyField);
        }

        protected virtual void DeclareAdvisedProxyInstanceField(TypeBuilder builder)
        {
            advisedProxyField = builder.DefineField("__advisedProxy", typeof(AdvisedProxy), FieldAttributes.Private);
        }

        ///// <summary>
        ///// 实现序列化房阿发
        ///// </summary>
        ///// <param name="typeBuilder"></param>
        //private void ImplementGetObjectDataMethod(TypeBuilder typeBuilder)
        //{
        //    typeBuilder.AddInterfaceImplementation(typeof(ISerializable));

        //    MethodBuilder mb =
        //        typeBuilder.DefineMethod("GetObjectData",
        //                                 MethodAttributes.Public | MethodAttributes.HideBySig | 
        //                                 MethodAttributes.NewSlot | MethodAttributes.Virtual,
        //                                 typeof (void),
        //                                 new Type[] {typeof (SerializationInfo), typeof (StreamingContext)});

        //    ILGenerator il = mb.GetILGenerator();
        //    il.Emit(OpCodes.Ldarg_1);
        //    il.Emit(OpCodes.Ldstr, "advisedProxy");
        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Ldfld, advisedProxyField);
        //    il.EmitCall(OpCodes.Callvirt, References.AddSerializationValue, null);
        //    il.Emit(OpCodes.Ret);

        //    //typeBuilder.DefineMethodOverride(mb, typeof(ISerializable).GetMethod("GetObjectData"));
        //}


        ///// <summary>
        ///// 实现序列化构造函数
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
        //    il.Emit(OpCodes.Ldstr, "advisedProxy");
        //    il.Emit(OpCodes.Ldtoken, typeof(AdvisedProxy));
        //    il.EmitCall(OpCodes.Call, References.GetTypeFromHandle, null);
        //    il.EmitCall(OpCodes.Callvirt, References.GetSerializationValue, null);
        //    il.Emit(OpCodes.Castclass, typeof(AdvisedProxy));
        //    il.Emit(OpCodes.Stfld, advisedProxyField);
        //    il.Emit(OpCodes.Ret);
        //}

        /// <summary>
        /// 实现代理类构造器
        /// </summary>
        protected override void ImplementConstructors(TypeBuilder typeBuilder)
        {
            ConstructorBuilder cb =
                typeBuilder.DefineConstructor(References.ObjectConstructor.Attributes,
                                              References.ObjectConstructor.CallingConvention,
                                              new Type[] { typeof(IAdvised) });

            ILGenerator il = cb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Ldarg_0); // kez 注释
            il.Emit(OpCodes.Newobj, References.AdvisedProxyConstructor);
            il.Emit(OpCodes.Stfld, advisedProxyField);
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// 实现 IDynamicProxy 接口.
        /// </summary>
        protected virtual void ImplementIAopProxy(TypeBuilder typeBuilder)
        {
            Type intf = typeof(IDynamicProxy);
            MethodInfo getProxyMethod = intf.GetMethod("GetProxy", Type.EmptyTypes);

            typeBuilder.AddInterfaceImplementation(intf);

            MethodBuilder mb = typeBuilder.DefineMethod(typeof(IAdvised).FullName + "." + getProxyMethod.Name,
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                getProxyMethod.CallingConvention, getProxyMethod.ReturnType, Type.EmptyTypes);

            ILGenerator il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(mb, getProxyMethod);
        }
      
        public static bool IsDecoratorProxy(Type type)
        {
            return type.FullName.StartsWith(PROXY_TYPE_NAME);
        }
    }
}