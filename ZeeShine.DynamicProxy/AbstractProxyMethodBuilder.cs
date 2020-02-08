using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using ZeeShine.Utils;

namespace ZeeShine.DynamicProxy
{
    /// <summary>
    /// 动态代理方法构建器基类
    /// </summary>
    public abstract class AbstractProxyMethodBuilder : IProxyMethodBuilder
    {
        protected TypeBuilder typeBuilder;

        /// <summary>
        /// 代理类型构建器
        /// </summary>
        protected IProxyTypeBuilder proxyTypeBuilder;

        /// <summary>
        /// 表面接口是否是被显示实现
        /// </summary>
        protected bool explicitImplementation;

        /// <summary>
        /// 目标对象的方法字典
        /// </summary>
        protected IDictionary targetMethods;

        /// <summary>
        /// 目标对象代理方法字典  
        /// </summary>
        protected IDictionary onProxyTargetMethods;

        // variables

        /// <summary>
        /// 存储拦截器的本地变量
        /// </summary>
        protected LocalBuilder interceptors;

        /// <summary>
        /// 存储被代理的目标对象实例的本地变量
        /// </summary>
        protected LocalBuilder targetType;

        /// <summary>
        /// 存储方法参数的本地变量
        /// </summary>
        protected LocalBuilder arguments;

        /// <summary>
        /// 存储方法返回值的本地变量
        /// </summary>
        protected LocalBuilder returnValue;

        /// <summary>
        /// 目标对象泛型方法
        /// </summary>
        protected LocalBuilder genericTargetMethod;

        /// <summary>
        /// 目标对象代理泛型方法 
        /// </summary>
        protected LocalBuilder genericOnProxyTargetMethod;

        /// <summary>
        /// 目标对象方法缓存字段
        /// </summary>
        protected FieldBuilder targetMethodCacheField;

        /// <summary>
        /// 目标对象代理方法缓存字段
        /// </summary>
        protected FieldBuilder onProxyTargetMethodCacheField;

        /// <summary>
        /// 方法是否有返回值
        /// </summary>
        protected bool methodReturnsValue;

        private static IDictionary ldindOpCodes;

        static AbstractProxyMethodBuilder()
        {
            ldindOpCodes = new Hashtable();
            ldindOpCodes[typeof(sbyte)] = OpCodes.Ldind_I1;
            ldindOpCodes[typeof(short)] = OpCodes.Ldind_I2;
            ldindOpCodes[typeof(int)] = OpCodes.Ldind_I4;
            ldindOpCodes[typeof(long)] = OpCodes.Ldind_I8;
            ldindOpCodes[typeof(byte)] = OpCodes.Ldind_U1;
            ldindOpCodes[typeof(ushort)] = OpCodes.Ldind_U2;
            ldindOpCodes[typeof(uint)] = OpCodes.Ldind_U4;
            ldindOpCodes[typeof(ulong)] = OpCodes.Ldind_I8;
            ldindOpCodes[typeof(float)] = OpCodes.Ldind_R4;
            ldindOpCodes[typeof(double)] = OpCodes.Ldind_R8;
            ldindOpCodes[typeof(char)] = OpCodes.Ldind_U2;
            ldindOpCodes[typeof(bool)] = OpCodes.Ldind_I1;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="typeBuilder">运行时类型构建器</param>
        /// <param name="proxyTypeBuilder">代理类型构建器</param>
        /// <param name="explicitImplementation">是否显示实现接口</param>
        /// <param name="targetMethods">目标对象方法字典</param>
        /// <param name="onProxyTargetMethods">代理目标对象的方法字典</param>
        public AbstractProxyMethodBuilder(TypeBuilder typeBuilder,
           IProxyTypeBuilder proxyTypeBuilder, bool explicitImplementation, IDictionary targetMethods, IDictionary onProxyTargetMethods)
        {
            this.typeBuilder = typeBuilder;
            this.proxyTypeBuilder = proxyTypeBuilder;
            this.explicitImplementation = explicitImplementation;
            this.targetMethods = targetMethods;
            this.onProxyTargetMethods = onProxyTargetMethods;
        }

        public AbstractProxyMethodBuilder(
         TypeBuilder typeBuilder, IProxyTypeBuilder proxyTypeBuilder,
         bool explicitImplementation, IDictionary targetMethods)
            : this(typeBuilder, proxyTypeBuilder, explicitImplementation, targetMethods, new Hashtable())
        {
        }

        /// <summary>
        /// 动态构建代理方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="interfaceMethod">接口方法</param>
        /// <returns></returns>
        public virtual MethodBuilder BuildProxyMethod(MethodInfo method, MethodInfo interfaceMethod)
        {
            MethodBuilder methodBuilder =
                DefineMethod(method, interfaceMethod, explicitImplementation);

            ILGenerator il = methodBuilder.GetILGenerator();

            GenerateMethod(il, method, interfaceMethod);

            il.Emit(OpCodes.Ret);

            if (explicitImplementation ||
                (interfaceMethod != null && interfaceMethod.Name != method.Name))
            {
                typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
            }

            return methodBuilder;
        }

        protected virtual void PushProxy(ILGenerator il)
        {
            proxyTypeBuilder.PushProxy(il);
        }

        protected virtual void PushTarget(ILGenerator il)
        {
            proxyTypeBuilder.PushTarget(il);
        }

        /// <summary>
        /// 为目标对象定义代理方法
        /// </summary>
        /// <param name="method">目标对象方法</param>
        /// <param name="intfMethod">接口方法</param>
        /// <param name="explicitImplementation">是否显示实现</param>
        /// <returns></returns>
        protected virtual MethodBuilder DefineMethod(
            MethodInfo method, MethodInfo intfMethod, bool explicitImplementation)
        {
            MethodBuilder methodBuilder;
            string name;
            MethodAttributes attributes;

            if (intfMethod == null)
            {
                name = method.Name;
                attributes = MethodAttributes.Public | MethodAttributes.ReuseSlot
                    | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            }
            else
            {
                attributes = MethodAttributes.HideBySig
                    | MethodAttributes.NewSlot | MethodAttributes.Virtual
                    | MethodAttributes.Final;

                if (explicitImplementation || method.Name.IndexOf('.') != -1)
                {
                    name = String.Format("{0}.{1}",
                        intfMethod.DeclaringType.FullName, intfMethod.Name);
                    attributes |= MethodAttributes.Private;
                }
                else
                {
                    name = intfMethod.Name;
                    attributes |= MethodAttributes.Public;
                }
            }

            if ((intfMethod != null && intfMethod.IsSpecialName) || method.IsSpecialName)
            {
                attributes |= MethodAttributes.SpecialName;
            }

            methodBuilder = typeBuilder.DefineMethod(name, attributes,
                method.CallingConvention, method.ReturnType,
                ReflectionUtils.GetParameterTypes(method.GetParameters()));

            DefineGenericParameters(methodBuilder, method);
            return methodBuilder;
        }

        /*
        protected void DefineParameters(MethodBuilder methodBuilder, MethodInfo method)
        {
            int n = 1;
            foreach (ParameterInfo param in method.GetParameters())
            {
                ParameterBuilder pb = methodBuilder.DefineParameter(n, param.Attributes, param.Name);
                n++;
            }
        }
        */

        /// <summary>
        /// 基于代理方法的元数据定义泛型方法参数
        /// </summary>
        /// <param name="methodBuilder">
        /// </param>
        /// <param name="method">被代理的方法</param>
        protected void DefineGenericParameters(MethodBuilder methodBuilder, MethodInfo method)
        {
            if (method.IsGenericMethodDefinition)
            {
                Type[] genericArguments = method.GetGenericArguments();

                // 定义泛型参数
                GenericTypeParameterBuilder[] gtpBuilders =
                    methodBuilder.DefineGenericParameters(ReflectionUtils.GetGenericParameterNames(genericArguments));

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    gtpBuilders[i].SetGenericParameterAttributes(genericArguments[i].GenericParameterAttributes);

                    Type[] constraints = genericArguments[i].GetGenericParameterConstraints();
                    System.Collections.Generic.List<Type> interfaces = new System.Collections.Generic.List<Type>(constraints.Length);
                    foreach (Type constraint in constraints)
                    {
                        if (constraint.IsClass)
                            gtpBuilders[i].SetBaseTypeConstraint(constraint);
                        else
                            interfaces.Add(constraint);
                    }
                    gtpBuilders[i].SetInterfaceConstraints(interfaces.ToArray());
                }
            }
        }

        /// <summary>
        /// 生成代理方法
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method">被代理的方法</param>
        /// <param name="interfaceMethod">接口定义的方法</param>
        //protected abstract void GenerateMethod(
        //    ILGenerator il, MethodInfo method, MethodInfo interfaceMethod);
        protected virtual void GenerateMethod(
         ILGenerator il, MethodInfo method, MethodInfo interfaceMethod)
        {
            methodReturnsValue = (method.ReturnType != typeof(void));

            DeclareLocals(il, method);

            GenerateTargetMethodCacheField(il, method);
            GenerateOnProxyTargetMethodCacheField(il, method);

            BeginMethod(il, method);
            GenerateMethodLogic(il, method, interfaceMethod);
            EndMethod(il, method);
        }

        /// <summary>
        /// 直接调用目标对象的方法
        /// </summary>
        /// <param name="il"></param>
        /// <param name="targetMethod">目标对象方法</param>
        protected virtual void CallDirectTargetMethod(
            ILGenerator il, MethodInfo targetMethod)
        {
            // 检查对象是否为空,接口类型是否支持
            // 为CallAssertUnderstands设置目标实例
            PushTarget(il);
            CallAssertUnderstands(il, targetMethod, "target");

            // 设置目标并且设置转换类型方法
            PushTarget(il);
            il.Emit(OpCodes.Castclass, targetMethod.DeclaringType);

            // 设置调用需要的参数
            ParameterInfo[] paramArray = targetMethod.GetParameters();
            for (int i = 0; i < paramArray.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_S, i + 1);
            }

            // 调用方法
            il.EmitCall(OpCodes.Callvirt, targetMethod, null);
        }

        /// <summary>
        /// 保证对象在站中处于方法下
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="targetName"></param>
        protected virtual void CallAssertUnderstands(ILGenerator il, MethodInfo method, string targetName)
        {
            il.Emit(OpCodes.Ldstr, targetName);
            il.Emit(OpCodes.Ldtoken, method.DeclaringType);
            il.Emit(OpCodes.Call, References.GetTypeFromHandleMethod);
            //il.Emit(OpCodes.Ldstr, string.Format("Interface method '{0}.{1}()' was not handled by any interceptor and the target does not implement this method.", method.DeclaringType.FullName, method.Name));
            il.Emit(OpCodes.Call, References.UnderstandsMethod);
        }

        /// <summary>
        /// 直接调用base 方法
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method">代理的方法</param>
        protected virtual void CallDirectBaseMethod(ILGenerator il, MethodInfo method)
        {
            // 检查对象是否为空,接口类型是否支持
            // 为CallAssertUnderstands设置代理实例
            PushProxy(il);
            CallAssertUnderstands(il, method, "base");

            // 设置代理并且设置转换类型方法
            PushProxy(il);
            il.Emit(OpCodes.Castclass, method.DeclaringType);

            // 设置调用需要的参数
            ParameterInfo[] paramArray = method.GetParameters();
            for (int i = 0; i < paramArray.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_S, i + 1);
            }

            // 调用方法
            il.EmitCall(OpCodes.Call, method, null);
        }

        /// <summary>
        /// 用代理的引用替换原始的引用
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="il"></param>
        /// <param name="returnValue">本地返回值</param>
        protected virtual void ProcessReturnValue(ILGenerator il, LocalBuilder returnValue)
        {
            Label jmpMethodReturn = il.DefineLabel();

            // 检查目标方法是否返回引用目标(this)
            PushTarget(il);
            il.Emit(OpCodes.Ldloc, returnValue);
            il.Emit(OpCodes.Bne_Un_S, jmpMethodReturn);

            // 检查代理是否能被转换成返回的类型
            PushProxy(il);
            il.Emit(OpCodes.Isinst, returnValue.LocalType);
            il.Emit(OpCodes.Brfalse_S, jmpMethodReturn);

            // 返回代理的引用
            PushProxy(il);
            il.Emit(OpCodes.Stloc, returnValue);

            il.MarkLabel(jmpMethodReturn);
        }

        /// <summary>
        /// 生成异常代码 <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="exceptionType">异常的类型</param>
        /// <param name="message"></param>
        protected static void EmitThrowException(ILGenerator il, Type exceptionType, string message)
        {
            ConstructorInfo NewException = exceptionType.GetConstructor(new Type[] { typeof(string) });

            il.Emit(OpCodes.Ldstr, message);
            il.Emit(OpCodes.Newobj, NewException);
            il.Emit(OpCodes.Throw);
        }

        /// <summary>
        /// 为缓存生成方法的唯一id
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        protected virtual string GenerateMethodCacheFieldId(MethodInfo method)
        {
            return "_m" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 创建缓存目标对象方法的静态字段
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void GenerateTargetMethodCacheField(
            ILGenerator il, MethodInfo method)
        {
            string methodId = GenerateMethodCacheFieldId(method);
            targetMethods.Add(methodId, method);

            targetMethodCacheField = typeBuilder.DefineField(methodId, typeof(MethodInfo),
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

            MakeGenericMethod(il, method, targetMethodCacheField, genericTargetMethod);
        }

        /// <summary>
        /// 创建缓存目标对象定义在代理上的方法的静态字段
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void GenerateOnProxyTargetMethodCacheField(
            ILGenerator il, MethodInfo method)
        {
        }

        /// <summary>
        /// 创建泛型方法
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="methodCacheField"></param>
        /// <param name="localMethod"></param>
        protected void MakeGenericMethod(ILGenerator il, MethodInfo method,
            FieldBuilder methodCacheField, LocalBuilder localMethod)
        {
            if (method.IsGenericMethodDefinition)
            {
                Type[] genericArgs = method.GetGenericArguments();

                LocalBuilder typeArgs = il.DeclareLocal(typeof(Type[]));

                il.Emit(OpCodes.Ldsfld, methodCacheField);

                // 指定数组大学并且创建数组
                il.Emit(OpCodes.Ldc_I4, genericArgs.Length);
                il.Emit(OpCodes.Newarr, typeof(Type));
                il.Emit(OpCodes.Stloc, typeArgs);

                // 用类型参数填充数组
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    il.Emit(OpCodes.Ldloc, typeArgs);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldtoken, genericArgs[i]);
                    il.EmitCall(OpCodes.Call, References.GetTypeFromHandle, null);
                    il.Emit(OpCodes.Stelem_Ref);
                }

                il.Emit(OpCodes.Ldloc, typeArgs);
                il.Emit(OpCodes.Callvirt, References.MakeGenericMethod);
                il.Emit(OpCodes.Stloc, localMethod);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="il"></param>
        protected virtual void PushTargetType(ILGenerator il)
        {
            proxyTypeBuilder.PushAdvisedProxy(il);
            il.Emit(OpCodes.Ldfld, References.TargetTypeField);
        }

        /// <summary>
        /// </summary>
        /// <param name="il"></param>
        protected virtual void PushAdvisedProxy(ILGenerator il)
        {
            proxyTypeBuilder.PushAdvisedProxy(il);
        }

        /// <summary>
        /// 将目标对象压栈
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void PushTargetMethodInfo(ILGenerator il, MethodInfo method)
        {
            if (method.IsGenericMethodDefinition)
            {
                il.Emit(OpCodes.Ldloc, genericTargetMethod);
                return;
            }
            il.Emit(OpCodes.Ldsfld, targetMethodCacheField);
        }

        /// <summary>
        /// 将定义在代理的目标对象压栈
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void PushOnProxyTargetMethodInfo(ILGenerator il, MethodInfo method)
        {
            if (onProxyTargetMethodCacheField != null)
            {
                if (method.IsGenericMethodDefinition)
                {
                    il.Emit(OpCodes.Ldloc, genericOnProxyTargetMethod);
                    return;
                }
                il.Emit(OpCodes.Ldsfld, onProxyTargetMethodCacheField);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
        }

        /// <summary>
        /// 创建本地变量定义
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void DeclareLocals(ILGenerator il, MethodInfo method)
        {
            interceptors = il.DeclareLocal(typeof(IList));
            targetType = il.DeclareLocal(typeof(Type));
            arguments = il.DeclareLocal(typeof(Object[]));

            if (method.IsGenericMethodDefinition)
            {
                genericTargetMethod = il.DeclareLocal(typeof(MethodInfo));
                genericOnProxyTargetMethod = il.DeclareLocal(typeof(MethodInfo));
            }
            if (methodReturnsValue)
            {
                returnValue = il.DeclareLocal(method.ReturnType);
            }

            //#if DEBUG
            //            interceptors.SetLocalSymInfo("interceptors");
            //            targetType.SetLocalSymInfo("targetType");
            //            arguments.SetLocalSymInfo("arguments");

            //            if (method.IsGenericMethodDefinition)
            //            {
            //                genericTargetMethod.SetLocalSymInfo("genericTargetMethod");
            //                genericOnProxyTargetMethod.SetLocalSymInfo("genericOnProxyTargetMethod");
            //            }

            //            if (methodReturnsValue)
            //            {
            //                returnValue.SetLocalSymInfo("returnValue");
            //            }
            //#endif
        }

        /// <summary>
        /// 初始化本地变量
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void BeginMethod(ILGenerator il, MethodInfo method)
        {
            Label jmpProxyNotExposed = il.DefineLabel();

            // 设置当前代理到对象
            PushAdvisedProxy(il);
            il.Emit(OpCodes.Ldfld, References.AdvisedField);
            il.EmitCall(OpCodes.Callvirt, References.ExposeProxyProperty, null);
            il.Emit(OpCodes.Brfalse_S, jmpProxyNotExposed);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, References.PushProxyMethod, null);

            il.MarkLabel(jmpProxyNotExposed);

            // 初始化目标类型
            PushTargetType(il);
            il.Emit(OpCodes.Stloc, targetType);

            // 初始化拦截器
            PushAdvisedProxy(il);
            il.Emit(OpCodes.Ldloc, targetType);
            PushTargetMethodInfo(il, method);
            il.EmitCall(OpCodes.Call, References.GetInterceptorsMethod, null);
            il.Emit(OpCodes.Stloc, interceptors);
        }

        /// <summary>
        /// 生成方法逻辑
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="interfaceMethod"></param>
        protected virtual void GenerateMethodLogic(
            ILGenerator il, MethodInfo method, MethodInfo interfaceMethod)
        {
            Label jmpDirectCall = il.DefineLabel();
            Label jmpEndIf = il.DefineLabel();

            // 检查是否有拦截器
            il.Emit(OpCodes.Ldloc, interceptors);
            il.EmitCall(OpCodes.Callvirt, References.CountProperty, null);
            il.Emit(OpCodes.Ldc_I4_0);

            // 如果没有拦截器,跳转到直接调用方法
            il.Emit(OpCodes.Ble, jmpDirectCall);

            // 否则调用Invok并跳转到方法末尾
            CallInvoke(il, method);
            il.Emit(OpCodes.Br, jmpEndIf);

            // 直接调用方法
            il.MarkLabel(jmpDirectCall);
            CallDirectProxiedMethod(il, method, interfaceMethod);
            if (methodReturnsValue)
            {
                // 存储返回值
                il.Emit(OpCodes.Stloc, returnValue);
            }

            il.MarkLabel(jmpEndIf);

            if (methodReturnsValue)
            {
                if (!method.ReturnType.IsValueType)
                {
                    ProcessReturnValue(il, returnValue);
                }
            }
        }

        /// <summary>
        /// 使用Invoke调用方法
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void CallInvoke(ILGenerator il, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();

            SetupMethodArguments(il, method, parameters);

            PushAdvisedProxy(il);

            // 为调用设置参数
            il.Emit(OpCodes.Ldarg_0);                       // proxy
            PushTarget(il);                                 // target
            il.Emit(OpCodes.Ldloc, targetType);             // target type
            PushTargetMethodInfo(il, method);               // method
            PushOnProxyTargetMethodInfo(il, method);        // method defined on proxy
            il.Emit(OpCodes.Ldloc, arguments);              // args
            il.Emit(OpCodes.Ldloc, interceptors);           // interceptors

            // 调用 Invoke方法
            il.EmitCall(OpCodes.Call, References.InvokeMethod, null);

            // 处理返回值
            if (methodReturnsValue)
            {
                EmitUnboxIfNeeded(il, method.ReturnType);
                il.Emit(OpCodes.Stloc, returnValue);
            }
            else
            {
                il.Emit(OpCodes.Pop);
            }

            // 处理传址类型的参数
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldarg_S, i + 1);
                    il.Emit(OpCodes.Ldloc, arguments);
                    il.Emit(OpCodes.Ldc_I4_S, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    Type type = parameters[i].ParameterType.GetElementType();
                    EmitUnboxIfNeeded(il, type);
                    EmitStoreValueIndirect(il, type);
                }
            }
        }

        /// <summary>
        /// 设置代理了的方法参数
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        protected void SetupMethodArguments(
            ILGenerator il, MethodInfo method, ParameterInfo[] parameters)
        {
            if (parameters.Length > 0)
            {
                // 指定数组大小并且创建数组
                il.Emit(OpCodes.Ldc_I4, parameters.Length);
                il.Emit(OpCodes.Newarr, typeof(Object));
                il.Emit(OpCodes.Stloc, arguments);

                // 使用参数填充数组
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type type = parameters[i].ParameterType;

                    il.Emit(OpCodes.Ldloc, arguments);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldarg_S, i + 1);

                    // 设置传址的参数
                    if (type.IsByRef)
                    {
                        type = type.GetElementType();
                        EmitLoadValueIndirect(il, type);
                    }

                    if (type.IsValueType || type.IsGenericParameter)
                    {
                        il.Emit(OpCodes.Box, type);
                    }

                    il.Emit(OpCodes.Stelem_Ref);
                }
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, arguments);
            }
        }

        /// <summary>
        /// 直接调用代理了的方法
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="interfaceMethod"></param>
        protected abstract void CallDirectProxiedMethod(
            ILGenerator il, MethodInfo method, MethodInfo interfaceMethod);

        /// <summary>
        /// 返回返回值
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void EndMethod(ILGenerator il, MethodInfo method)
        {
            Label jmpProxyNotExposed = il.DefineLabel();

            // 重置当前代理为旧值
            PushAdvisedProxy(il);
            il.Emit(OpCodes.Ldfld, References.AdvisedField);
            il.EmitCall(OpCodes.Callvirt, References.ExposeProxyProperty, null);
            il.Emit(OpCodes.Brfalse_S, jmpProxyNotExposed);
            il.EmitCall(OpCodes.Call, References.PopProxyMethod, null);

            il.MarkLabel(jmpProxyNotExposed);

            if (methodReturnsValue)
            {
                il.Emit(OpCodes.Ldloc, returnValue);
            }
        }

        /// <summary>
        /// 使用IL指令加载指定的值到评估栈
        /// </summary>
        /// <param name="il"></param>
        /// <param name="type"></param>
        protected static void EmitLoadValueIndirect(ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                if (type == typeof(int)) il.Emit(OpCodes.Ldind_I4);
                else if (type == typeof(uint)) il.Emit(OpCodes.Ldind_U4);
                else if (type == typeof(char)) il.Emit(OpCodes.Ldind_I2);
                else if (type == typeof(bool)) il.Emit(OpCodes.Ldind_I1);
                else if (type == typeof(float)) il.Emit(OpCodes.Ldind_R4);
                else if (type == typeof(double)) il.Emit(OpCodes.Ldind_R8);
                else if (type == typeof(short)) il.Emit(OpCodes.Ldind_I2);
                else if (type == typeof(ushort)) il.Emit(OpCodes.Ldind_U2);
                else if (type == typeof(long) || type == typeof(ulong)) il.Emit(OpCodes.Ldind_I8);
                else il.Emit(OpCodes.Ldobj, type);
            }
            else
            {
                il.Emit(OpCodes.Ldind_Ref);
            }
        }

        /// <summary>
        /// 使用IL指令存储指定的值到提供的地址空间
        /// </summary>
        /// <param name="il"></param>
        /// <param name="type"></param>
        protected static void EmitStoreValueIndirect(ILGenerator il, Type type)
        {
            if (type.IsValueType)
            {
                if (type.IsEnum) EmitStoreValueIndirect(il, Enum.GetUnderlyingType(type));
                else if (type == typeof(int)) il.Emit(OpCodes.Stind_I4);
                else if (type == typeof(short)) il.Emit(OpCodes.Stind_I2);
                else if (type == typeof(long) || type == typeof(ulong)) il.Emit(OpCodes.Stind_I8);
                else if (type == typeof(char)) il.Emit(OpCodes.Stind_I2);
                else if (type == typeof(bool)) il.Emit(OpCodes.Stind_I1);
                else if (type == typeof(float)) il.Emit(OpCodes.Stind_R4);
                else if (type == typeof(double)) il.Emit(OpCodes.Stind_R8);
                else il.Emit(OpCodes.Stobj, type);
            }
            else
            {
                il.Emit(OpCodes.Stind_Ref);
            }
        }

        /// <summary>
        /// 使用IL指令拆箱
        /// </summary>
        /// <param name="il"></param>
        /// <param name="type"></param>
        protected static void EmitUnboxIfNeeded(ILGenerator il, Type type)
        {
            if (type.IsValueType || type.IsGenericParameter)
            {
                il.Emit(OpCodes.Unbox_Any, type);
            }
        }
    }
}
