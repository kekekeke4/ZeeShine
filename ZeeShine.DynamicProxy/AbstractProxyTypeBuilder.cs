using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using ZeeShine.Utils;

namespace ZeeShine.DynamicProxy
{
	/// <summary>
	/// 代理类型构建器基类
	/// </summary>
	public abstract class AbstractProxyTypeBuilder : IProxyTypeBuilder
	{
        private const string DEFAULT_PROXY_TYPE_NAME = "ZeeShineDynamicProxy";
        private string _name;
		private Type _targetType;
		private Type _baseType = typeof (object);
        private IList<Type> _interfaces;
        private bool _proxyTargetAttributes = true;
		private IList _typeAttributes = new ArrayList();
		private IDictionary _memberAttributes = new Hashtable();

        /// <summary>
        /// 创建代理类型
        /// </summary>
        /// <returns></returns>
        public abstract Type BuildProxyType();

        /// <summary>
        /// 获取或设置代理名称
        /// </summary>
        /// <value></value>
        public string Name
        {
            get 
            {
                if (StringUtils.IsNullOrEmpty(_name))
                {
                    _name = DEFAULT_PROXY_TYPE_NAME;
                }
                return _name; 
            }
            set { _name = value; }
        }

        /// <summary>
        /// 获取或设置目标类型
        /// </summary>
        public Type TargetType
        {
            get { return _targetType; }
            set { _targetType = value; }
        }

        /// <summary>
        /// 获取或设置基类类型
        /// </summary>
        public Type BaseType
        {
            get { return _baseType; }
            set { _baseType = value; }
        }

        /// <summary>
        /// 获取或设置代理要实现的接口列表
        /// </summary>
        public IList<Type> Interfaces
        {
            get
            {
                if (_interfaces == null)
                {
                    _interfaces = GetProxiableInterfaces(TargetType.GetInterfaces());
                }
                return _interfaces;
            }
            set { _interfaces = value; }
        }

        /// <summary>
        /// 获取或设置是否代理目标对象的特性
        /// </summary>
        public bool ProxyTargetAttributes
        {
            get { return _proxyTargetAttributes; }
            set { _proxyTargetAttributes = value; }
        }

        /// <summary>
        /// 获取或设置类型的特性列表
        /// </summary>
        /// <see cref="IProxyTypeBuilder.TypeAttributes"/>
        public IList TypeAttributes
        {
            get { return _typeAttributes; }
            set { _typeAttributes = value; }
        }

        /// <summary>
        /// 获取或设置成员的自定义特性列表
        /// </summary>
        public IDictionary MemberAttributes
        {
            get { return _memberAttributes; }
            set { _memberAttributes = value; }
        }

        /// <summary>
        /// 使用IL指令将代理的实例压占
        /// </summary>
        /// <param name="il"></param>
        public virtual void PushProxy(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_0);
        }

        /// <summary>
        /// </summary>
        /// <param name="il"></param>
        public virtual void PushTarget(ILGenerator il)
        {
            PushAdvisedProxy(il);
            //il.Emit(OpCodes.Ldfld, References.TargetSourceField);
            //il.EmitCall(OpCodes.Callvirt, References.GetTargetMethod, null);
            il.EmitCall(OpCodes.Call, References.GetTargetMethod, null);
        }


        /// <summary>
        /// 生成IL指令将当前AdvisedProxy代理实例压栈
        /// </summary>
        /// <param name="il"></param>
        public abstract void PushAdvisedProxy(ILGenerator il);


        /// <summary>
        /// 创建类型构建器
        /// </summary>
        /// <param name="name"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        protected virtual TypeBuilder CreateTypeBuilder(string name, Type baseType)
        {
            // 生成唯一的类型名称
            string typeName = String.Format("{0}_{1}", 
                name, Guid.NewGuid().ToString("N"));

            return DynamicProxyManager.CreateTypeBuilder(typeName, baseType);
        }

        /// <summary>
        /// 给代理类应用特性
		/// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="targetType"></param>
		protected virtual void ApplyTypeAttributes(TypeBuilder typeBuilder, Type targetType)
		{
            foreach (object attr in GetTypeAttributes(targetType))
			{
				if (attr is CustomAttributeBuilder)
				{
                    typeBuilder.SetCustomAttribute((CustomAttributeBuilder)attr);
				}
                else if (attr is CustomAttributeData)
                {
                    typeBuilder.SetCustomAttribute(
                        ReflectionUtils.CreateCustomAttribute((CustomAttributeData)attr));
                }
				else if (attr is Attribute)
				{
                    typeBuilder.SetCustomAttribute(ReflectionUtils.CreateCustomAttribute((Attribute)attr));
				}
			}
		}

        /// <summary>
        /// 给被代理的方法应用特性
        /// </summary>
        /// <param name="methodBuilder"></param>
        /// <param name="targetMethod"></param>
        protected virtual void ApplyMethodAttributes(MethodBuilder methodBuilder, MethodInfo targetMethod)
		{
            foreach (object attr in GetMethodAttributes(targetMethod))
            {
                if (attr is CustomAttributeBuilder)
                {
                    methodBuilder.SetCustomAttribute((CustomAttributeBuilder)attr);
                }
                else if (attr is CustomAttributeData)
                {
                    methodBuilder.SetCustomAttribute(
                        ReflectionUtils.CreateCustomAttribute((CustomAttributeData)attr));
                }
                else if (attr is Attribute)
                {
                    methodBuilder.SetCustomAttribute(
                        ReflectionUtils.CreateCustomAttribute((Attribute)attr));
                }
            }

            ApplyMethodReturnTypeAttributes(methodBuilder, targetMethod);
            ApplyMethodParameterAttributes(methodBuilder, targetMethod);
        }

        /// <summary>
        /// 给被代理的方法返回值应用特性
        /// </summary>
        /// <param name="methodBuilder"></param>
        /// <param name="targetMethod"></param>
        protected virtual void ApplyMethodReturnTypeAttributes(MethodBuilder methodBuilder, MethodInfo targetMethod)
        {
            ParameterBuilder parameterBuilder = methodBuilder.DefineParameter(0, ParameterAttributes.Retval, null);
            foreach (object attr in GetMethodReturnTypeAttributes(targetMethod))
            {
                if (attr is CustomAttributeBuilder)
                {
                    parameterBuilder.SetCustomAttribute((CustomAttributeBuilder)attr);
                }
                else if (attr is CustomAttributeData)
                {
                    parameterBuilder.SetCustomAttribute(
                        ReflectionUtils.CreateCustomAttribute((CustomAttributeData)attr));
                }
                else if (attr is Attribute)
                {
                    parameterBuilder.SetCustomAttribute(
                        ReflectionUtils.CreateCustomAttribute((Attribute)attr));
                }
            }
        }

        /// <summary>
        /// 给被代理的方法参数应用特性
        /// </summary>
        /// <param name="methodBuilder"></param>
        /// <param name="targetMethod"></param>
        protected virtual void ApplyMethodParameterAttributes(MethodBuilder methodBuilder, MethodInfo targetMethod)
        {
            foreach (ParameterInfo paramInfo in targetMethod.GetParameters())
            {
                ParameterBuilder parameterBuilder = methodBuilder.DefineParameter(
                    (paramInfo.Position + 1), paramInfo.Attributes, paramInfo.Name);
                foreach (object attr in GetMethodParameterAttributes(targetMethod, paramInfo))
                {
                    if (attr is CustomAttributeBuilder)
                    {
                        parameterBuilder.SetCustomAttribute((CustomAttributeBuilder)attr);
                    }
                    else if (attr is CustomAttributeData)
                    {
                        parameterBuilder.SetCustomAttribute(
                            ReflectionUtils.CreateCustomAttribute((CustomAttributeData)attr));
                    }
                    else if (attr is Attribute)
                    {
                        parameterBuilder.SetCustomAttribute(
                            ReflectionUtils.CreateCustomAttribute((Attribute)attr));
                    }
                }
            }
        }

        /// <summary>
        /// 计算并返回指定类型的特性列表
        /// </summary>
        /// <param name="type"></param>
        protected virtual IList GetTypeAttributes(Type type)
        {
            ArrayList attributes = new ArrayList();

            if (this.ProxyTargetAttributes && !type.Equals(typeof(object)))
            {
                // 给目标类型添加特性
                attributes.AddRange(ReflectionUtils.GetCustomAttributes(type));
            }

            // 根据配置添加特性定义
            attributes.AddRange(TypeAttributes);

            return attributes;
        }

        /// <summary>
        /// 计算并返回指定方法的特性列表
        /// </summary>
        /// <param name="method"></param>
        protected virtual IList GetMethodAttributes(MethodInfo method)
        {
            ArrayList attributes = new ArrayList();

            if (this.ProxyTargetAttributes && 
                !method.DeclaringType.IsInterface)
            {
                // 添加目标方法特性
                attributes.AddRange(ReflectionUtils.GetCustomAttributes(method));
            }

            // 根据定义添加特性定义
            foreach (DictionaryEntry entry in MemberAttributes)
            {
                if (ReflectionUtils.MethodMatch((string)entry.Key, method))
                {
                    if (entry.Value is Attribute)
                    {
                        attributes.Add(entry.Value);
                    }
                    else if (entry.Value is IList)
                    {
                        attributes.AddRange(entry.Value as IList);
                    }
                }
            }

            return attributes;
        }

        /// <summary>
        /// 计算并返回指定方法返回值的特性列表
        /// </summary>
        /// <param name="method"></param>
        protected virtual IList GetMethodReturnTypeAttributes(MethodInfo method)
        {
            ArrayList attributes = new ArrayList();

            if (this.ProxyTargetAttributes &&
                !method.DeclaringType.IsInterface)
            {
                // 添加目标方法返回值特性
                object[] attrs = method.ReturnTypeCustomAttributes.GetCustomAttributes(false);
                try
                {
                    System.Collections.Generic.IList<CustomAttributeData> attrsData = 
                        CustomAttributeData.GetCustomAttributes(method.ReturnParameter);
                    
                    if (attrs.Length != attrsData.Count)
                    {
                        attributes.AddRange(attrs);
                    }
                    else
                    {
                        foreach (CustomAttributeData cad in attrsData)
                        {
                            attributes.Add(cad);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    attributes.AddRange(attrs);
                }
            }

            return attributes;
        }

        /// <summary>
        /// 计算并返回指定方法参数的特性列表
        /// </summary>
        /// <param name="method"></param>
        /// <param name="paramInfo"></param>
        protected virtual IList GetMethodParameterAttributes(MethodInfo method, ParameterInfo paramInfo)
        {
            ArrayList attributes = new ArrayList();

            if (this.ProxyTargetAttributes &&
                !method.DeclaringType.IsInterface)
            {
                // 给目标方法参数添加特性
                object[] attrs = paramInfo.GetCustomAttributes(false);
                try
                {
                    IList<CustomAttributeData> attrsData = CustomAttributeData.GetCustomAttributes(paramInfo);
                    
                    if (attrs.Length != attrsData.Count)
                    {
                        attributes.AddRange(attrs);
                    }
                    else
                    {
                        foreach (CustomAttributeData cad in attrsData)
                        {
                            attributes.Add(cad);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    attributes.AddRange(attrs);
                }
            }

            return attributes;
        }

        protected virtual bool IsAttributeMatchingType(object attr, Type attrType)
        {
            if (attr is Attribute)
            {
                return (attrType == attr.GetType());
            }
            else if (attr is CustomAttributeData)
            {
                return (attrType == ((CustomAttributeData)attr).Constructor.DeclaringType);
            }
            else if (attr is CustomAttributeBuilder)
            {
                return (attrType == ((ConstructorInfo)CustomAttributeConstructorField.GetValue(attr)).DeclaringType);
            }
            return false;
        }

        private static readonly FieldInfo CustomAttributeConstructorField = 
            typeof(CustomAttributeBuilder).GetField("m_con", BindingFlags.Instance | BindingFlags.NonPublic);

        protected virtual Type[] DefineConstructorParameters(ConstructorInfo constructor)
        {
            return ReflectionUtils.GetParameterTypes(constructor.GetParameters());
        }

        protected virtual void ImplementConstructors(TypeBuilder typeBuilder)
        {
            ConstructorInfo[] constructors = TargetType.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (ConstructorInfo constructor in constructors)
            {
                if (constructor.IsPublic || constructor.IsFamily)
                {
                    ConstructorBuilder cb = typeBuilder.DefineConstructor(
                        constructor.Attributes,
                        constructor.CallingConvention, 
                        DefineConstructorParameters(constructor));

                    ILGenerator il = cb.GetILGenerator();
                    GenerateConstructor(cb, il, constructor);
                    il.Emit(OpCodes.Ret);
                }
            }
        }

        /// <summary>
        /// 生成代理的构造函数
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="il"></param>
        /// <param name="constructor"></param>
        protected virtual void GenerateConstructor(
            ConstructorBuilder builder, ILGenerator il, ConstructorInfo constructor)
        {
        }

        /// <summary>
        /// 实现接口
		/// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="proxyMethodBuilder"></param>
        /// <param name="intf"></param>
        /// <param name="targetType"></param>
        protected virtual void ImplementInterface(TypeBuilder typeBuilder, 
            IProxyMethodBuilder proxyMethodBuilder, Type intf, Type targetType)
        {
            ImplementInterface(typeBuilder, proxyMethodBuilder, intf, targetType, true);
        }

        /// <summary>
        /// 实现接口
		/// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="proxyMethodBuilder"></param>
        /// <param name="intf"></param>
        /// <param name="targetType"></param>
        /// <param name="proxyVirtualMethods"></param>
		protected virtual void ImplementInterface(TypeBuilder typeBuilder,
            IProxyMethodBuilder proxyMethodBuilder, Type intf, 
            Type targetType, bool proxyVirtualMethods)
		{
            Dictionary<string, MethodBuilder> methodMap = new Dictionary<string, MethodBuilder>();

            InterfaceMapping mapping = GetInterfaceMapping(targetType, intf);

            typeBuilder.AddInterfaceImplementation(intf);

            for (int i = 0; i < mapping.InterfaceMethods.Length; i++)
			{
                if (!proxyVirtualMethods && 
                    !mapping.TargetMethods[i].DeclaringType.IsInterface &&
                    mapping.TargetMethods[i].IsVirtual &&
                    !mapping.TargetMethods[i].IsFinal)
                    continue;

                MethodBuilder methodBuilder = proxyMethodBuilder.BuildProxyMethod(
                    mapping.TargetMethods[i], mapping.InterfaceMethods[i]);

                ApplyMethodAttributes(methodBuilder, mapping.TargetMethods[i]);

                methodMap[mapping.InterfaceMethods[i].Name] = methodBuilder;
			}
			foreach (PropertyInfo property in intf.GetProperties())
			{
                ImplementProperty(typeBuilder, intf, property, methodMap);
			}
			foreach (EventInfo evt in intf.GetEvents())
			{
                ImplementEvent(typeBuilder, intf, evt, methodMap);
			}
		}

        /// <summary>
        /// 获取接口映射
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="intf"></param>
        protected virtual InterfaceMapping GetInterfaceMapping(
            Type targetType, Type intf)
        {
            InterfaceMapping mapping;

            if (intf.IsAssignableFrom(targetType))
            {
                // 目标类型实现这接口
                mapping = targetType.GetInterfaceMap(intf);
            }
            else
            {
                // 目标对象未实现这接口
                mapping.TargetType = targetType;
                mapping.InterfaceType = intf;
                mapping.InterfaceMethods = intf.GetMethods();
                mapping.TargetMethods = mapping.InterfaceMethods;
            }

            return mapping;
        }

        /// <summary>
        /// 从指定类型继承
		/// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="proxyMethodBuilder"></param>
        /// <param name="type"></param>
        protected virtual void InheritType(TypeBuilder typeBuilder, 
            IProxyMethodBuilder proxyMethodBuilder, Type type)
        {
            InheritType(typeBuilder, proxyMethodBuilder, type, false);
        }

        /// <summary>
        /// 从指定类型继承
		/// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="proxyMethodBuilder"></param>
        /// <param name="type"></param>
        /// <param name="declaredMembersOnly"></param>
        protected virtual void InheritType(TypeBuilder typeBuilder,
            IProxyMethodBuilder proxyMethodBuilder, Type type, bool declaredMembersOnly)
        {
            IDictionary<string, MethodBuilder> methodMap = new Dictionary<string, MethodBuilder>();

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            if (declaredMembersOnly)
            {
                bindingFlags |= BindingFlags.DeclaredOnly;
            }

            // 重写虚方法
            MethodInfo[] methods = type.GetMethods(bindingFlags);
            foreach (MethodInfo method in methods)
            {
                MethodAttributes memberAccess = method.Attributes & MethodAttributes.MemberAccessMask;

                if (method.IsVirtual && !method.IsFinal && !method.Name.Equals("Finalize")
                    && (memberAccess == MethodAttributes.Public || memberAccess == MethodAttributes.Family || memberAccess == MethodAttributes.FamORAssem))
                {
                    MethodBuilder methodBuilder = proxyMethodBuilder.BuildProxyMethod(method, null);
                    ApplyMethodAttributes(methodBuilder, method);
                    methodMap[method.Name] = methodBuilder;
                }
            }

            // 重写虚属性
            foreach (PropertyInfo property in type.GetProperties(bindingFlags))
            {
                ImplementProperty(typeBuilder, type, property, methodMap);
            }

            // 重写虚事件
            foreach (EventInfo evt in type.GetEvents(bindingFlags))
            {
                ImplementEvent(typeBuilder, type, evt, methodMap);
            }
        }

		/// <summary>
        /// 实现属性
		/// </summary>
		/// <param name="typeBuilder"></param>
		/// <param name="type"></param>
		/// <param name="property"></param>
		/// <param name="methodMap"></param>
		protected virtual void ImplementProperty(
			TypeBuilder typeBuilder, Type type, PropertyInfo property, IDictionary<string, MethodBuilder> methodMap)
		{
            MethodBuilder getMethod;
            methodMap.TryGetValue("get_" + property.Name, out getMethod);
		    MethodBuilder setMethod;
            methodMap.TryGetValue("set_" + property.Name, out setMethod);

            if (getMethod != null || setMethod != null)
            {
                string propertyName = (type.IsInterface && 
                                      ((getMethod != null && getMethod.IsPrivate) || (setMethod != null && setMethod.IsPrivate)))
                                        ? type.FullName + "." + property.Name
                                        : property.Name;
                PropertyBuilder pb = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None,
                                                property.PropertyType, null);

                // 设置 get/set 方法
                if (property.CanRead && getMethod != null)
                {
                    pb.SetGetMethod(getMethod);
                }
                if (property.CanWrite && setMethod != null)
                {
                    pb.SetSetMethod(setMethod);
                }
            }
		}

	    /// <summary>
        /// 实现事件
	    /// </summary>
	    /// <param name="typeBuilder"></param>
	    /// <param name="type"></param>
	    /// <param name="evt"></param>
	    /// <param name="methodMap"></param>
	    protected virtual void ImplementEvent(TypeBuilder typeBuilder, Type type, EventInfo evt, IDictionary<string, MethodBuilder> methodMap)
		{
            MethodBuilder addOnMethod;
            methodMap.TryGetValue("add_" + evt.Name, out addOnMethod);
            MethodBuilder removeOnMethod;
	        methodMap.TryGetValue("remove_" + evt.Name, out removeOnMethod);

            if (addOnMethod != null && removeOnMethod != null)
            {
                string eventName = (addOnMethod.IsPrivate) 
                    ? addOnMethod.DeclaringType.FullName + "." + evt.Name 
                    : evt.Name;

                EventBuilder eb = typeBuilder.DefineEvent(
                    eventName, EventAttributes.None, evt.EventHandlerType);
                
                // 设置 add/remove 方法
                eb.SetAddOnMethod(addOnMethod);
                eb.SetRemoveOnMethod(removeOnMethod);
            }		
		}

	    /// <summary>
        /// 返回可代理接口数组
	    /// </summary>
	    /// <param name="interfaces"></param>
	    protected virtual IList<Type> GetProxiableInterfaces(IList<Type> interfaces)
        {
            List<Type>  proxiableInterfaces = new List<Type>();

            foreach(Type intf in interfaces)
            {
                if (!Attribute.IsDefined(intf, typeof(ProxyIgnoreAttribute), false) &&
                    !IsSpecialInterface(intf) &&
                    ReflectionUtils.IsTypeVisible(intf, DynamicProxyManager.ASSEMBLY_NAME))
                {
                    if (!proxiableInterfaces.Contains(intf))
                    {
                        proxiableInterfaces.Add(intf);
                    }

                    Type[] baseInterfaces = intf.GetInterfaces();
                    foreach (Type baseInterface in baseInterfaces)
                    {
                        if (!proxiableInterfaces.Contains(baseInterface))
                        {
                            proxiableInterfaces.Add(baseInterface);
                        }
                    }
                }
            }

            return proxiableInterfaces;
        }

        /// <summary>
        /// 检查指定的接口是否是用于不被代理的
        /// </summary>
        /// <param name="intf"></param>
        private bool IsSpecialInterface(Type intf)
	    {
	        return intf == typeof(ISerializable);
	    }
    }
}