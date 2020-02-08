using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using ZeeShine.Utils;

namespace ZeeShine.DynamicProxy
{
    /// <summary>
    /// ��̬����������������
    /// </summary>
    public abstract class AbstractProxyMethodBuilder : IProxyMethodBuilder
    {
        protected TypeBuilder typeBuilder;

        /// <summary>
        /// �������͹�����
        /// </summary>
        protected IProxyTypeBuilder proxyTypeBuilder;

        /// <summary>
        /// ����ӿ��Ƿ��Ǳ���ʾʵ��
        /// </summary>
        protected bool explicitImplementation;

        /// <summary>
        /// Ŀ�����ķ����ֵ�
        /// </summary>
        protected IDictionary targetMethods;

        /// <summary>
        /// Ŀ�����������ֵ�  
        /// </summary>
        protected IDictionary onProxyTargetMethods;

        // variables

        /// <summary>
        /// �洢�������ı��ر���
        /// </summary>
        protected LocalBuilder interceptors;

        /// <summary>
        /// �洢�������Ŀ�����ʵ���ı��ر���
        /// </summary>
        protected LocalBuilder targetType;

        /// <summary>
        /// �洢���������ı��ر���
        /// </summary>
        protected LocalBuilder arguments;

        /// <summary>
        /// �洢��������ֵ�ı��ر���
        /// </summary>
        protected LocalBuilder returnValue;

        /// <summary>
        /// Ŀ������ͷ���
        /// </summary>
        protected LocalBuilder genericTargetMethod;

        /// <summary>
        /// Ŀ���������ͷ��� 
        /// </summary>
        protected LocalBuilder genericOnProxyTargetMethod;

        /// <summary>
        /// Ŀ����󷽷������ֶ�
        /// </summary>
        protected FieldBuilder targetMethodCacheField;

        /// <summary>
        /// Ŀ���������������ֶ�
        /// </summary>
        protected FieldBuilder onProxyTargetMethodCacheField;

        /// <summary>
        /// �����Ƿ��з���ֵ
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
        /// ���캯��
        /// </summary>
        /// <param name="typeBuilder">����ʱ���͹�����</param>
        /// <param name="proxyTypeBuilder">�������͹�����</param>
        /// <param name="explicitImplementation">�Ƿ���ʾʵ�ֽӿ�</param>
        /// <param name="targetMethods">Ŀ����󷽷��ֵ�</param>
        /// <param name="onProxyTargetMethods">����Ŀ�����ķ����ֵ�</param>
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
        /// ��̬����������
        /// </summary>
        /// <param name="method"></param>
        /// <param name="interfaceMethod">�ӿڷ���</param>
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
        /// ΪĿ������������
        /// </summary>
        /// <param name="method">Ŀ����󷽷�</param>
        /// <param name="intfMethod">�ӿڷ���</param>
        /// <param name="explicitImplementation">�Ƿ���ʾʵ��</param>
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
        /// ���ڴ�������Ԫ���ݶ��巺�ͷ�������
        /// </summary>
        /// <param name="methodBuilder">
        /// </param>
        /// <param name="method">������ķ���</param>
        protected void DefineGenericParameters(MethodBuilder methodBuilder, MethodInfo method)
        {
            if (method.IsGenericMethodDefinition)
            {
                Type[] genericArguments = method.GetGenericArguments();

                // ���巺�Ͳ���
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
        /// ���ɴ�����
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method">������ķ���</param>
        /// <param name="interfaceMethod">�ӿڶ���ķ���</param>
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
        /// ֱ�ӵ���Ŀ�����ķ���
        /// </summary>
        /// <param name="il"></param>
        /// <param name="targetMethod">Ŀ����󷽷�</param>
        protected virtual void CallDirectTargetMethod(
            ILGenerator il, MethodInfo targetMethod)
        {
            // �������Ƿ�Ϊ��,�ӿ������Ƿ�֧��
            // ΪCallAssertUnderstands����Ŀ��ʵ��
            PushTarget(il);
            CallAssertUnderstands(il, targetMethod, "target");

            // ����Ŀ�겢������ת�����ͷ���
            PushTarget(il);
            il.Emit(OpCodes.Castclass, targetMethod.DeclaringType);

            // ���õ�����Ҫ�Ĳ���
            ParameterInfo[] paramArray = targetMethod.GetParameters();
            for (int i = 0; i < paramArray.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_S, i + 1);
            }

            // ���÷���
            il.EmitCall(OpCodes.Callvirt, targetMethod, null);
        }

        /// <summary>
        /// ��֤������վ�д��ڷ�����
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
        /// ֱ�ӵ���base ����
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method">����ķ���</param>
        protected virtual void CallDirectBaseMethod(ILGenerator il, MethodInfo method)
        {
            // �������Ƿ�Ϊ��,�ӿ������Ƿ�֧��
            // ΪCallAssertUnderstands���ô���ʵ��
            PushProxy(il);
            CallAssertUnderstands(il, method, "base");

            // ���ô���������ת�����ͷ���
            PushProxy(il);
            il.Emit(OpCodes.Castclass, method.DeclaringType);

            // ���õ�����Ҫ�Ĳ���
            ParameterInfo[] paramArray = method.GetParameters();
            for (int i = 0; i < paramArray.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_S, i + 1);
            }

            // ���÷���
            il.EmitCall(OpCodes.Call, method, null);
        }

        /// <summary>
        /// �ô���������滻ԭʼ������
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="il"></param>
        /// <param name="returnValue">���ط���ֵ</param>
        protected virtual void ProcessReturnValue(ILGenerator il, LocalBuilder returnValue)
        {
            Label jmpMethodReturn = il.DefineLabel();

            // ���Ŀ�귽���Ƿ񷵻�����Ŀ��(this)
            PushTarget(il);
            il.Emit(OpCodes.Ldloc, returnValue);
            il.Emit(OpCodes.Bne_Un_S, jmpMethodReturn);

            // �������Ƿ��ܱ�ת���ɷ��ص�����
            PushProxy(il);
            il.Emit(OpCodes.Isinst, returnValue.LocalType);
            il.Emit(OpCodes.Brfalse_S, jmpMethodReturn);

            // ���ش��������
            PushProxy(il);
            il.Emit(OpCodes.Stloc, returnValue);

            il.MarkLabel(jmpMethodReturn);
        }

        /// <summary>
        /// �����쳣���� <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="exceptionType">�쳣������</param>
        /// <param name="message"></param>
        protected static void EmitThrowException(ILGenerator il, Type exceptionType, string message)
        {
            ConstructorInfo NewException = exceptionType.GetConstructor(new Type[] { typeof(string) });

            il.Emit(OpCodes.Ldstr, message);
            il.Emit(OpCodes.Newobj, NewException);
            il.Emit(OpCodes.Throw);
        }

        /// <summary>
        /// Ϊ�������ɷ�����Ψһid
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        protected virtual string GenerateMethodCacheFieldId(MethodInfo method)
        {
            return "_m" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// ��������Ŀ����󷽷��ľ�̬�ֶ�
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
        /// ��������Ŀ��������ڴ����ϵķ����ľ�̬�ֶ�
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void GenerateOnProxyTargetMethodCacheField(
            ILGenerator il, MethodInfo method)
        {
        }

        /// <summary>
        /// �������ͷ���
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

                // ָ�������ѧ���Ҵ�������
                il.Emit(OpCodes.Ldc_I4, genericArgs.Length);
                il.Emit(OpCodes.Newarr, typeof(Type));
                il.Emit(OpCodes.Stloc, typeArgs);

                // �����Ͳ����������
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
        /// ��Ŀ�����ѹջ
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
        /// �������ڴ����Ŀ�����ѹջ
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
        /// �������ر�������
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
        /// ��ʼ�����ر���
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void BeginMethod(ILGenerator il, MethodInfo method)
        {
            Label jmpProxyNotExposed = il.DefineLabel();

            // ���õ�ǰ��������
            PushAdvisedProxy(il);
            il.Emit(OpCodes.Ldfld, References.AdvisedField);
            il.EmitCall(OpCodes.Callvirt, References.ExposeProxyProperty, null);
            il.Emit(OpCodes.Brfalse_S, jmpProxyNotExposed);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, References.PushProxyMethod, null);

            il.MarkLabel(jmpProxyNotExposed);

            // ��ʼ��Ŀ������
            PushTargetType(il);
            il.Emit(OpCodes.Stloc, targetType);

            // ��ʼ��������
            PushAdvisedProxy(il);
            il.Emit(OpCodes.Ldloc, targetType);
            PushTargetMethodInfo(il, method);
            il.EmitCall(OpCodes.Call, References.GetInterceptorsMethod, null);
            il.Emit(OpCodes.Stloc, interceptors);
        }

        /// <summary>
        /// ���ɷ����߼�
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="interfaceMethod"></param>
        protected virtual void GenerateMethodLogic(
            ILGenerator il, MethodInfo method, MethodInfo interfaceMethod)
        {
            Label jmpDirectCall = il.DefineLabel();
            Label jmpEndIf = il.DefineLabel();

            // ����Ƿ���������
            il.Emit(OpCodes.Ldloc, interceptors);
            il.EmitCall(OpCodes.Callvirt, References.CountProperty, null);
            il.Emit(OpCodes.Ldc_I4_0);

            // ���û��������,��ת��ֱ�ӵ��÷���
            il.Emit(OpCodes.Ble, jmpDirectCall);

            // �������Invok����ת������ĩβ
            CallInvoke(il, method);
            il.Emit(OpCodes.Br, jmpEndIf);

            // ֱ�ӵ��÷���
            il.MarkLabel(jmpDirectCall);
            CallDirectProxiedMethod(il, method, interfaceMethod);
            if (methodReturnsValue)
            {
                // �洢����ֵ
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
        /// ʹ��Invoke���÷���
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void CallInvoke(ILGenerator il, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();

            SetupMethodArguments(il, method, parameters);

            PushAdvisedProxy(il);

            // Ϊ�������ò���
            il.Emit(OpCodes.Ldarg_0);                       // proxy
            PushTarget(il);                                 // target
            il.Emit(OpCodes.Ldloc, targetType);             // target type
            PushTargetMethodInfo(il, method);               // method
            PushOnProxyTargetMethodInfo(il, method);        // method defined on proxy
            il.Emit(OpCodes.Ldloc, arguments);              // args
            il.Emit(OpCodes.Ldloc, interceptors);           // interceptors

            // ���� Invoke����
            il.EmitCall(OpCodes.Call, References.InvokeMethod, null);

            // ������ֵ
            if (methodReturnsValue)
            {
                EmitUnboxIfNeeded(il, method.ReturnType);
                il.Emit(OpCodes.Stloc, returnValue);
            }
            else
            {
                il.Emit(OpCodes.Pop);
            }

            // ����ַ���͵Ĳ���
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
        /// ���ô����˵ķ�������
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        protected void SetupMethodArguments(
            ILGenerator il, MethodInfo method, ParameterInfo[] parameters)
        {
            if (parameters.Length > 0)
            {
                // ָ�������С���Ҵ�������
                il.Emit(OpCodes.Ldc_I4, parameters.Length);
                il.Emit(OpCodes.Newarr, typeof(Object));
                il.Emit(OpCodes.Stloc, arguments);

                // ʹ�ò����������
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type type = parameters[i].ParameterType;

                    il.Emit(OpCodes.Ldloc, arguments);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldarg_S, i + 1);

                    // ���ô�ַ�Ĳ���
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
        /// ֱ�ӵ��ô����˵ķ���
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="interfaceMethod"></param>
        protected abstract void CallDirectProxiedMethod(
            ILGenerator il, MethodInfo method, MethodInfo interfaceMethod);

        /// <summary>
        /// ���ط���ֵ
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        protected virtual void EndMethod(ILGenerator il, MethodInfo method)
        {
            Label jmpProxyNotExposed = il.DefineLabel();

            // ���õ�ǰ����Ϊ��ֵ
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
        /// ʹ��ILָ�����ָ����ֵ������ջ
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
        /// ʹ��ILָ��洢ָ����ֵ���ṩ�ĵ�ַ�ռ�
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
        /// ʹ��ILָ�����
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
