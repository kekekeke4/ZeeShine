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
	/// �������͹���������
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
        /// ������������
        /// </summary>
        /// <returns></returns>
        public abstract Type BuildProxyType();

        /// <summary>
        /// ��ȡ�����ô�������
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
        /// ��ȡ������Ŀ������
        /// </summary>
        public Type TargetType
        {
            get { return _targetType; }
            set { _targetType = value; }
        }

        /// <summary>
        /// ��ȡ�����û�������
        /// </summary>
        public Type BaseType
        {
            get { return _baseType; }
            set { _baseType = value; }
        }

        /// <summary>
        /// ��ȡ�����ô���Ҫʵ�ֵĽӿ��б�
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
        /// ��ȡ�������Ƿ����Ŀ����������
        /// </summary>
        public bool ProxyTargetAttributes
        {
            get { return _proxyTargetAttributes; }
            set { _proxyTargetAttributes = value; }
        }

        /// <summary>
        /// ��ȡ���������͵������б�
        /// </summary>
        /// <see cref="IProxyTypeBuilder.TypeAttributes"/>
        public IList TypeAttributes
        {
            get { return _typeAttributes; }
            set { _typeAttributes = value; }
        }

        /// <summary>
        /// ��ȡ�����ó�Ա���Զ��������б�
        /// </summary>
        public IDictionary MemberAttributes
        {
            get { return _memberAttributes; }
            set { _memberAttributes = value; }
        }

        /// <summary>
        /// ʹ��ILָ������ʵ��ѹռ
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
        /// ����ILָ���ǰAdvisedProxy����ʵ��ѹջ
        /// </summary>
        /// <param name="il"></param>
        public abstract void PushAdvisedProxy(ILGenerator il);


        /// <summary>
        /// �������͹�����
        /// </summary>
        /// <param name="name"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        protected virtual TypeBuilder CreateTypeBuilder(string name, Type baseType)
        {
            // ����Ψһ����������
            string typeName = String.Format("{0}_{1}", 
                name, Guid.NewGuid().ToString("N"));

            return DynamicProxyManager.CreateTypeBuilder(typeName, baseType);
        }

        /// <summary>
        /// ��������Ӧ������
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
        /// ��������ķ���Ӧ������
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
        /// ��������ķ�������ֵӦ������
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
        /// ��������ķ�������Ӧ������
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
        /// ���㲢����ָ�����͵������б�
        /// </summary>
        /// <param name="type"></param>
        protected virtual IList GetTypeAttributes(Type type)
        {
            ArrayList attributes = new ArrayList();

            if (this.ProxyTargetAttributes && !type.Equals(typeof(object)))
            {
                // ��Ŀ�������������
                attributes.AddRange(ReflectionUtils.GetCustomAttributes(type));
            }

            // ��������������Զ���
            attributes.AddRange(TypeAttributes);

            return attributes;
        }

        /// <summary>
        /// ���㲢����ָ�������������б�
        /// </summary>
        /// <param name="method"></param>
        protected virtual IList GetMethodAttributes(MethodInfo method)
        {
            ArrayList attributes = new ArrayList();

            if (this.ProxyTargetAttributes && 
                !method.DeclaringType.IsInterface)
            {
                // ���Ŀ�귽������
                attributes.AddRange(ReflectionUtils.GetCustomAttributes(method));
            }

            // ���ݶ���������Զ���
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
        /// ���㲢����ָ����������ֵ�������б�
        /// </summary>
        /// <param name="method"></param>
        protected virtual IList GetMethodReturnTypeAttributes(MethodInfo method)
        {
            ArrayList attributes = new ArrayList();

            if (this.ProxyTargetAttributes &&
                !method.DeclaringType.IsInterface)
            {
                // ���Ŀ�귽������ֵ����
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
        /// ���㲢����ָ�����������������б�
        /// </summary>
        /// <param name="method"></param>
        /// <param name="paramInfo"></param>
        protected virtual IList GetMethodParameterAttributes(MethodInfo method, ParameterInfo paramInfo)
        {
            ArrayList attributes = new ArrayList();

            if (this.ProxyTargetAttributes &&
                !method.DeclaringType.IsInterface)
            {
                // ��Ŀ�귽�������������
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
        /// ���ɴ���Ĺ��캯��
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="il"></param>
        /// <param name="constructor"></param>
        protected virtual void GenerateConstructor(
            ConstructorBuilder builder, ILGenerator il, ConstructorInfo constructor)
        {
        }

        /// <summary>
        /// ʵ�ֽӿ�
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
        /// ʵ�ֽӿ�
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
        /// ��ȡ�ӿ�ӳ��
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="intf"></param>
        protected virtual InterfaceMapping GetInterfaceMapping(
            Type targetType, Type intf)
        {
            InterfaceMapping mapping;

            if (intf.IsAssignableFrom(targetType))
            {
                // Ŀ������ʵ����ӿ�
                mapping = targetType.GetInterfaceMap(intf);
            }
            else
            {
                // Ŀ�����δʵ����ӿ�
                mapping.TargetType = targetType;
                mapping.InterfaceType = intf;
                mapping.InterfaceMethods = intf.GetMethods();
                mapping.TargetMethods = mapping.InterfaceMethods;
            }

            return mapping;
        }

        /// <summary>
        /// ��ָ�����ͼ̳�
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
        /// ��ָ�����ͼ̳�
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

            // ��д�鷽��
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

            // ��д������
            foreach (PropertyInfo property in type.GetProperties(bindingFlags))
            {
                ImplementProperty(typeBuilder, type, property, methodMap);
            }

            // ��д���¼�
            foreach (EventInfo evt in type.GetEvents(bindingFlags))
            {
                ImplementEvent(typeBuilder, type, evt, methodMap);
            }
        }

		/// <summary>
        /// ʵ������
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

                // ���� get/set ����
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
        /// ʵ���¼�
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
                
                // ���� add/remove ����
                eb.SetAddOnMethod(addOnMethod);
                eb.SetRemoveOnMethod(removeOnMethod);
            }		
		}

	    /// <summary>
        /// ���ؿɴ���ӿ�����
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
        /// ���ָ���Ľӿ��Ƿ������ڲ��������
        /// </summary>
        /// <param name="intf"></param>
        private bool IsSpecialInterface(Type intf)
	    {
	        return intf == typeof(ISerializable);
	    }
    }
}