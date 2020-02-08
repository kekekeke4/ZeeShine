using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace ZeeShine.Utils
{
    public sealed class ReflectionUtils
    {
        public const BindingFlags AllMembersCaseInsensitiveFlags = BindingFlags.Public |
                                                                   BindingFlags.NonPublic | BindingFlags.Instance
                                                                   | BindingFlags.Static
                                                                   | BindingFlags.IgnoreCase;

        static ReflectionUtils()
        { }

        public static bool IsSimpleType(Type type)
        {
            return type == typeof(short) ||
                   type == typeof(short?) ||
                   type == typeof(ushort) ||
                   type == typeof(ushort?) ||
                   type == typeof(int) ||
                   type == typeof(int?) ||
                   type == typeof(uint) ||
                   type == typeof(uint?) ||
                   type == typeof(long) ||
                   type == typeof(long?) ||
                   type == typeof(ulong) ||
                   type == typeof(ulong?) ||
                   type == typeof(decimal) ||
                   type == typeof(decimal?) ||
                   type == typeof(byte) ||
                   type == typeof(byte?) ||
                   type == typeof(sbyte) ||
                   type == typeof(sbyte?) ||
                   type == typeof(float) ||
                   type == typeof(float?) ||
                   type == typeof(double) ||
                   type == typeof(double?) ||
                   type == typeof(bool) ||
                   type == typeof(bool?) ||
                   type == typeof(char) ||
                   type == typeof(char?) ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTime?) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(TimeSpan?) ||
                   type == typeof(Guid) ||
                   type == typeof(Guid?) ||
                   type.IsEnum;
        }

        public static Attribute FindAttribute(Type type, Type attributeType)
        {
            Attribute[] attributes = Attribute.GetCustomAttributes(type, attributeType, false);  
            if (attributes.Length > 0)
            {
                return attributes[0];
            }
            foreach (Type interfaceType in type.GetInterfaces())
            {
                Attribute attrib = FindAttribute(interfaceType, attributeType);
                if (attrib != null)
                {
                    return attrib;
                }
            }
            if (type.BaseType == null)
            {
                return null;
            }
            return FindAttribute(type.BaseType, attributeType);
        }

        public static bool IsNullableType(Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        
        public static string GetSignature(
            Type type, string method, Type[] argumentTypes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(type.FullName).Append("::").Append(method).Append("(");
            string separator = "";
            for (int i = 0; i < argumentTypes.Length; i++)
            {
                sb.Append(separator).Append(argumentTypes[i].FullName);
                separator = ",";
            }
            sb.Append(")");
            return sb.ToString();
        }


        public static MethodInfo GetMethod(
                    Type targetType, string method, Type[] argumentTypes)
        {
            return GetMethod(targetType, method, argumentTypes, 0);
        }

        
        public static MethodInfo GetMethod(
            Type targetType, string method, Type[] argumentTypes, int genericArgumentsCount)
        {
            AssertUtils.ArgumentNotNull(targetType, "Type must not be null");

            MethodInfo retMethod = null;

            MethodInfo[] methods = targetType.GetMethods(ReflectionUtils.AllMembersCaseInsensitiveFlags);

            foreach (MethodInfo candidate in methods)
            {
                if (candidate.Name.ToLower() == method.ToLower())
                {
                    Type[] parameterTypes = Array.ConvertAll<ParameterInfo, Type>(candidate.GetParameters(), delegate (ParameterInfo i) { return i.ParameterType; });
                    bool typesMatch = false;

                    bool zeroTypeArguments = null == argumentTypes || argumentTypes.Length == 0;

                    if (!zeroTypeArguments && parameterTypes.Length == argumentTypes.Length)
                    {
                        for (int i = 0; i < parameterTypes.Length; i++)
                        {
                            typesMatch = parameterTypes[i] == argumentTypes[i];
                            if (!typesMatch)
                            {
                                break;
                            }
                        }
                    }

                    if (typesMatch || zeroTypeArguments)
                    {
                        if (candidate.GetGenericArguments().Length == genericArgumentsCount)
                        {
                            retMethod = candidate;
                            break;
                        }
                    }
                }
            }


            if (retMethod == null)
            {
                int idx = method.LastIndexOf('.');
                if (idx > -1)
                {
                    method = method.Substring(idx + 1);
                    retMethod = ReflectionUtils.GetMethod(targetType, method, argumentTypes);
                }
            }
            return retMethod;
        }

        public static MethodInfo MapInterfaceMethodToImplementationIfNecessary(MethodInfo methodInfo, System.Type implementingType)
        {
            AssertUtils.ArgumentNotNull(methodInfo, "methodInfo");
            AssertUtils.ArgumentNotNull(implementingType, "implementingType");
            AssertUtils.IsTrue(methodInfo.DeclaringType.IsAssignableFrom(implementingType), "methodInfo and implementingType are unrelated");

            MethodInfo concreteMethodInfo = methodInfo;

            if (methodInfo.DeclaringType.IsInterface)
            {
                InterfaceMapping interfaceMapping = implementingType.GetInterfaceMap(methodInfo.DeclaringType);
                int methodIndex = Array.IndexOf(interfaceMapping.InterfaceMethods, methodInfo);
                concreteMethodInfo = interfaceMapping.TargetMethods[methodIndex];
            }

            return concreteMethodInfo;
        }

      
        public static Type[] GetParameterTypes(MethodBase method)
        {
            AssertUtils.ArgumentNotNull(method, "method");
            return GetParameterTypes(method.GetParameters());
        }

        
        public static Type[] GetParameterTypes(ParameterInfo[] args)
        {
            AssertUtils.ArgumentNotNull(args, "args");
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                types[i] = args[i].ParameterType;
            }
            return types;
        }

        
        public static string[] GetGenericParameterNames(MethodInfo method)
        {
            AssertUtils.ArgumentNotNull(method, "method");
            return GetGenericParameterNames(method.GetGenericArguments());
        }

        public static string[] GetGenericParameterNames(Type[] args)
        {
            AssertUtils.ArgumentNotNull(args, "args");
            string[] names = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                names[i] = args[i].Name;
            }
            return names;
        }

        public static MethodInfo GetGenericMethod(Type type, string methodName, Type[] typeArguments, Type[] parameterTypes)
        {
            MethodInfo methodInfo = null;

            if (typeArguments == null)
            {
                // 非泛型方法
                methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
            }
            else
            {
                // 泛型方法
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (MethodInfo method in methods)
                {
                    if (method.Name != methodName)
                    {
                        continue;
                    }

                    if (!method.IsGenericMethod)
                    {
                        continue;
                    }

                    // 比较方法参数
                    bool paramsOk = false;
                    if (method.GetParameters().Length == parameterTypes.Length)
                    {
                        paramsOk = true;
                        for (int i = 0; i < method.GetParameters().Length; i++)
                        {
                            if (method.GetParameters()[i].ParameterType != parameterTypes[i])
                            {
                                paramsOk = false;
                                break;
                            }
                        }
                    }
                    if (!paramsOk)
                    {
                        continue;
                    }

                    // 检测泛型参数
                    bool argsOk = false;
                    if (method.GetGenericArguments().Length == typeArguments.Length)
                    {
                        argsOk = true;
                    }

                    if (!argsOk)
                    {
                        continue;
                    }

                    methodInfo = method.MakeGenericMethod(typeArguments);
                    break;
                }
            }
            return methodInfo;
        }

        public static MethodInfo GetMethodByArgumentValues<T>(IEnumerable<T> methods, object[] argValues) where T : MethodBase
        {
            return (MethodInfo)GetMethodBaseByArgumentValues("method", methods, argValues);
        }

        private static MethodBase GetMethodBaseByArgumentValues<T>(string methodTypeName, IEnumerable<T> methods, object[] argValues) where T : MethodBase
        {
            MethodBase match = null;
            int matchCount = 0;

            foreach (MethodBase m in methods)
            {
                ParameterInfo[] parameters = m.GetParameters();
                bool isMatch = true;
                bool isExactMatch = true;
                object[] paramValues = (argValues == null) ? new object[0] : argValues;

                try
                {
                    if (parameters.Length > 0)
                    {
                        ParameterInfo lastParameter = parameters[parameters.Length - 1];
                        if (lastParameter.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0 && argValues.Length >= parameters.Length)
                        {
                            paramValues =
                                PackageParamArray(argValues, parameters.Length,
                                                  lastParameter.ParameterType.GetElementType());
                        }
                    }

                    if (parameters.Length != paramValues.Length)
                    {
                        isMatch = false;
                    }
                    else
                    {
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            Type paramType = parameters[i].ParameterType;
                            object paramValue = paramValues[i];

                            if ((paramValue == null && paramType.IsValueType && !IsNullableType(paramType))
                                || (paramValue != null && !paramType.IsAssignableFrom(paramValue.GetType())))
                            {
                                isMatch = false;
                                break;
                            }

                            if (paramValue == null || paramType != paramValue.GetType())
                            {
                                isExactMatch = false;
                            }
                        }
                    }
                }
                catch (InvalidCastException)
                {
                    isMatch = false;
                }

                if (isMatch)
                {
                    if (isExactMatch)
                    {
                        return m;
                    }

                    matchCount++;
                    if (matchCount == 1)
                    {
                        match = m;
                    }
                    else
                    {
                        throw new AmbiguousMatchException(
                            string.Format("Ambiguous match for {0} '{1}' for the specified number and types of arguments.", methodTypeName,
                                          m.Name));
                    }
                }
            }

            return match;
        }

        public static ConstructorInfo GetConstructorByArgumentValues<T>(IList<T> methods, object[] argValues) where T : MethodBase
        {
            return (ConstructorInfo)GetMethodBaseByArgumentValues("constructor", methods, argValues);
        }

        public static object[] PackageParamArray(object[] argValues, int argCount, Type elementType)
        {
            object[] values = new object[argCount];
            int i = 0;

            while (i < argCount - 1)
            {
                values[i] = argValues[i];
                i++;
            }

            Array paramArray = Array.CreateInstance(elementType, argValues.Length - i);
            int j = 0;
            while (i < argValues.Length)
            {
                paramArray.SetValue(argValues[i++], j++);
            }
            values[values.Length - 1] = paramArray;

            return values;
        }

        public static IList<Type> ToInterfaceArray(Type intf)
        {
            AssertUtils.ArgumentNotNull(intf, "intf");

            if (!intf.IsInterface)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture,
                                  "[{0}] is a class.",
                                  intf.FullName));
            }

            List<Type> interfaces = new List<Type>(intf.GetInterfaces());
            interfaces.Add(intf);

            return interfaces;
        }

        public static bool PropertyIsIndexer(string propertyName, Type type)
        {
            DefaultMemberAttribute[] attribs =
                (DefaultMemberAttribute[])type.GetCustomAttributes(typeof(DefaultMemberAttribute), true);
            if (attribs.Length != 0)
            {
                foreach (DefaultMemberAttribute attrib in attribs)
                {
                    if (attrib.MemberName.Equals(propertyName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool MethodIsOnOneOfTheseInterfaces(MethodBase method, Type[] interfaces)
        {
            AssertUtils.ArgumentNotNull(method, "method");
            if (interfaces == null)
            {
                return false;
            }
            Type[] paramTypes = GetParameterTypes(method.GetParameters());
            for (int i = 0; i < interfaces.Length; i++)
            {
                Type interfaceType = interfaces[i];
                AssertUtils.ArgumentNotNull(interfaceType, StringUtils.Surround("interfaces[", i, "]"));
                if (!interfaceType.IsInterface)
                {
                    throw new ArgumentException(interfaces[i].FullName + " is not an interface");
                }
                try
                {
                    MethodInfo mi = interfaceType.GetMethod(
                        method.Name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                        null, paramTypes, null);
                    if (mi != null)
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }

        public static object GetDefaultValue(Type type)
        {
            if (!type.IsValueType)
            {
                return null;
            }
            if (type == typeof(Boolean))
            {
                return false;
            }
            if (type == typeof(DateTime))
            {
                return DateTime.MinValue;
            }
            if (type == typeof(Char))
            {
                return Char.MinValue;
            }
            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                if (values == null || values.Length == 0)
                {
                    throw new ArgumentException("Bad 'enum' Type : cannot get default value because 'enum' has no values.");
                }
                return values.GetValue(0);
            }
            return 0;
        }

        public static object[] GetDefaultValues(Type[] types)
        {
            object[] defaults = new object[types.Length];
            for (int i = 0; i < types.Length; ++i)
            {
                defaults[i] = GetDefaultValue(types[i]);
            }
            return defaults;
        }

        public static bool ParameterTypesMatch(
            MethodInfo candidate, Type[] parameterTypes)
        {
            AssertUtils.ArgumentNotNull(candidate, "candidate");
            AssertUtils.ArgumentNotNull(parameterTypes, "parameterTypes");
            Type[] candidatesParameterTypes
                = ReflectionUtils.GetParameterTypes(candidate);
            if (candidatesParameterTypes.Length != parameterTypes.Length)
            {
                return false;
            }
            for (int i = 0; i < candidatesParameterTypes.Length; ++i)
            {
                if (!candidatesParameterTypes[i].Equals(parameterTypes[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static Type[] GetTypes(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return Type.EmptyTypes;
            }
            Type[] paramsType = new Type[args.Length];
            for (int i = 0; i < args.Length; ++i)
            {
                object arg = args[i];
                paramsType[i] = (arg != null) ? args[i].GetType() : typeof(object);
            }
            return paramsType;
        }


        public static bool HasAtLeastOneMethodWithName(Type type, string name)
        {
            if (type == null || StringUtils.IsNullOrEmpty(name))
            {
                return false;
            }
            return MethodCountForName(type, name) > 0;
        }

        public static int MethodCountForName(Type type, string name)
        {
            AssertUtils.ArgumentNotNull(type, "type", "Type must not be null");
            AssertUtils.ArgumentNotNull(name, "name", "Method name must not be null");
            MemberInfo[] methods = type.FindMembers(
                MemberTypes.Method,
                ReflectionUtils.AllMembersCaseInsensitiveFlags,
                new MemberFilter(ReflectionUtils.MethodNameFilter),
                name);
            return methods.Length;
        }

        private static bool MethodNameFilter(MemberInfo member, object criteria)
        {
            MethodInfo method = member as MethodInfo;
            string name = criteria as string;
            return String.Compare(method.Name, name, true, CultureInfo.InvariantCulture) == 0;
        }

        public static CustomAttributeBuilder CreateCustomAttribute(
            Type type, object[] ctorArgs, Attribute sourceAttribute)
        {
            #region Sanity Checks

            AssertUtils.ArgumentNotNull(type, "type");
            if (!typeof(Attribute).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    string.Format("[{0}] does not derive from the [System.Attribute] class.",
                                  type.FullName));
            }

            #endregion

            ConstructorInfo ci = type.GetConstructor(ReflectionUtils.GetTypes(ctorArgs));
            if (ci == null && ctorArgs.Length == 0)
            {
                ci = type.GetConstructors()[0];
                ctorArgs = GetDefaultValues(GetParameterTypes(ci.GetParameters()));
            }

            if (sourceAttribute != null)
            {
                object defaultAttribute = null;
                try
                {
                    defaultAttribute = ci.Invoke(ctorArgs);
                }
                catch
                {
                }

                IList<PropertyInfo> getSetProps = new List<PropertyInfo>();
                IList getSetValues = new ArrayList();
                IList<PropertyInfo> readOnlyProps = new List<PropertyInfo>();
                IList readOnlyValues = new ArrayList();
                foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (pi.DeclaringType == typeof(Attribute))
                        continue;

                    if (pi.CanRead)
                    {
                        if (pi.CanWrite)
                        {
                            object propValue = pi.GetValue(sourceAttribute, null);
                            if (defaultAttribute != null)
                            {
                                object defaultValue = pi.GetValue(defaultAttribute, null);
                                if ((propValue == null && defaultValue == null) ||
                                    (propValue != null && propValue.Equals(defaultValue)))
                                    continue;
                            }
                            getSetProps.Add(pi);
                            getSetValues.Add(propValue);
                        }
                        else
                        {
                            readOnlyProps.Add(pi);
                            readOnlyValues.Add(pi.GetValue(sourceAttribute, null));
                        }
                    }
                }

                if (readOnlyProps.Count == 1)
                {
                    PropertyInfo pi = readOnlyProps[0];
                    ConstructorInfo ciTemp = type.GetConstructor(new Type[1] { pi.PropertyType });
                    if (ciTemp != null)
                    {
                        ci = ciTemp;
                        ctorArgs = new object[1] { readOnlyValues[0] };
                    }
                    else
                    {
                        ciTemp = type.GetConstructor(new Type[1] { readOnlyValues[0].GetType() });
                        if (ciTemp != null)
                        {
                            ci = ciTemp;
                            ctorArgs = new object[1] { readOnlyValues[0] };
                        }
                    }
                }

                PropertyInfo[] propertyInfos = new PropertyInfo[getSetProps.Count];
                getSetProps.CopyTo(propertyInfos, 0);

                object[] propertyValues = new object[getSetValues.Count];
                getSetValues.CopyTo(propertyValues, 0);

                return new CustomAttributeBuilder(ci, ctorArgs, propertyInfos, propertyValues);
            }
            else
            {
                return new CustomAttributeBuilder(ci, ctorArgs);
            }
        }

        public static CustomAttributeBuilder CreateCustomAttribute(
            Type type, Attribute sourceAttribute)
        {
            return CreateCustomAttribute(type, new object[] { }, sourceAttribute);
        }

        public static CustomAttributeBuilder CreateCustomAttribute(Attribute sourceAttribute)
        {
            return CreateCustomAttribute(sourceAttribute.GetType(), sourceAttribute);
        }

        public static CustomAttributeBuilder CreateCustomAttribute(Type type)
        {
            return CreateCustomAttribute(type, new object[] { }, null);
        }

        public static CustomAttributeBuilder CreateCustomAttribute(
            Type type, params object[] ctorArgs)
        {
            return CreateCustomAttribute(type, ctorArgs, null);
        }

        public static CustomAttributeBuilder CreateCustomAttribute(CustomAttributeData attributeData)
        {
            object[] parameterValues = new object[attributeData.ConstructorArguments.Count];
            Type[] parameterTypes = new Type[attributeData.ConstructorArguments.Count];

            IList namedParameterValues = new ArrayList();
            IList namedFieldValues = new ArrayList();

            for (int i = 0; i < attributeData.ConstructorArguments.Count; i++)
            {
                parameterTypes[i] = attributeData.ConstructorArguments[i].ArgumentType;
                parameterValues[i] = ConvertConstructorArgsToObjectArrayIfNecessary(attributeData.ConstructorArguments[i].Value);
            }

            Type attributeType = attributeData.Constructor.DeclaringType;
            PropertyInfo[] attributeProperties = attributeType.GetProperties(
                BindingFlags.Instance | BindingFlags.Public);
            FieldInfo[] attributeFields = attributeType.GetFields(
                BindingFlags.Instance | BindingFlags.Public);

            IList propertiesToSet = new ArrayList();

            IList fieldsToSet = new ArrayList();

            foreach (CustomAttributeNamedArgument namedArgument in attributeData.NamedArguments)
            {
                bool noMatchingProperty = false;

                for (int j = 0; j < attributeProperties.Length; j++)
                {
                    if (attributeProperties[j].Name == namedArgument.MemberInfo.Name)
                    {
                        propertiesToSet.Add(attributeProperties[j]);
                        namedParameterValues.Add(ConvertConstructorArgsToObjectArrayIfNecessary(namedArgument.TypedValue.Value));
                        break;
                    }
                    else
                    {
                        if (j == attributeProperties.Length - 1)
                        {
                            noMatchingProperty = true;
                        }
                    }
                }
                if (noMatchingProperty)
                {
                    for (int j = 0; j < attributeFields.Length; j++)
                    {
                        if (attributeFields[j].Name == namedArgument.MemberInfo.Name)
                        {
                            fieldsToSet.Add(attributeFields[j]);
                            namedFieldValues.Add(ConvertConstructorArgsToObjectArrayIfNecessary(namedArgument.TypedValue.Value));
                            break;
                        }
                        else
                        {
                            if (j == attributeFields.Length - 1)
                            {
                                throw new InvalidOperationException(
                                        String.Format(CultureInfo.InvariantCulture,
                                        "A property or public field with name {0} can't be found in the " +
                                        "type {1}, but is present as a named property " +
                                        "on the attributeData {2}", namedArgument.MemberInfo.Name,
                                        attributeType.FullName, attributeData));
                            }
                        }
                    }
                }
            }

            ConstructorInfo constructor = attributeType.GetConstructor(parameterTypes);

            PropertyInfo[] namedProperties = new PropertyInfo[propertiesToSet.Count];
            propertiesToSet.CopyTo(namedProperties, 0);

            object[] propertyValues = new object[namedParameterValues.Count];
            namedParameterValues.CopyTo(propertyValues, 0);

            if (fieldsToSet.Count == 0)
            {
                return new CustomAttributeBuilder(
                        constructor, parameterValues, namedProperties, propertyValues);
            }
            else
            {
                FieldInfo[] namedFields = new FieldInfo[fieldsToSet.Count];
                fieldsToSet.CopyTo(namedFields, 0);

                object[] fieldValues = new object[namedFieldValues.Count];
                namedFieldValues.CopyTo(fieldValues, 0);

                return new CustomAttributeBuilder(
                        constructor, parameterValues, namedProperties, propertyValues, namedFields, fieldValues);
            }
        }

        private static object ConvertConstructorArgsToObjectArrayIfNecessary(object value)
        {
            if (value == null)
                return value;

            IList<CustomAttributeTypedArgument> constructorArguments = value as IList<CustomAttributeTypedArgument>;

            if (constructorArguments == null)
                return value;

            object[] arguments = new object[constructorArguments.Count];

            for (int i = 0; i < constructorArguments.Count; i++)
            {
                arguments[i] = constructorArguments[i].Value;
            }

            return arguments;
        }

        public static IList GetCustomAttributes(MemberInfo member)
        {
            ArrayList attributes = new ArrayList();
            object[] attrs = member.GetCustomAttributes(false);
            try
            {
                IList<CustomAttributeData> attrsData = CustomAttributeData.GetCustomAttributes(member);
                if (attrs.Length != attrsData.Count)
                {
                    attributes.AddRange(attrs);
                }
                else if (attrsData.Count > 0)
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
            catch (CustomAttributeFormatException)
            {
                attributes.AddRange(attrs);
            }
            return attributes;
        }

        public static IList<T> GetCustomAttributes<T>(MemberInfo member) where T : Attribute
        {
            var result = new List<T>();
            var attrs = GetCustomAttributes(member);
            foreach (var attr in attrs)
            {
                if (attr is T)
                {
                    result.Add((T)attr);
                }
            }
            return result;
        }

        public static T GetCustomAttribute<T>(MemberInfo member) where T : Attribute
        {
            var attrs = GetCustomAttributes<T>(member);
            return attrs.FirstOrDefault();
        }

        public static IList<T> GetCustomAttributes<T>(Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttributes<T>(inherit).ToList();
        }

        public static T GetCustomAttribute<T>(Type type, bool inherit = false) where T : Attribute
        {
            return GetCustomAttributes<T>(type, inherit).FirstOrDefault();
        }

        public static MethodInfo[] GetMatchingMethods(Type type, MethodInfo[] methods, bool strict)
        {
            AssertUtils.ArgumentNotNull(type, "type");
            AssertUtils.ArgumentNotNull(methods, "methods");

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            MethodInfo[] matched = new MethodInfo[methods.Length];
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                MethodInfo match = type.GetMethod(method.Name, flags, null, ReflectionUtils.GetParameterTypes(method), null);
                if ((match == null || match.ReturnType != method.ReturnType) && strict)
                {
                    throw new Exception(
                        string.Format("Method '{0}' could not be matched in the target class [{1}].",
                                      method.Name, type.FullName));
                }
                matched[i] = match;
            }
            return matched;
        }

        public static Type TypeOfOrType(object source)
        {
            return source is Type ? source as Type : source.GetType();
        }

        private static readonly MethodInfo Exception_InternalPreserveStackTrace =
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);

        public static Exception UnwrapTargetInvocationException(TargetInvocationException ex)
        {
            //if (SystemUtils.MonoRuntime)
            //{
            //    return ex.InnerException;
            //}
            Exception_InternalPreserveStackTrace.Invoke(ex.InnerException, new Object[] { });
            return ex.InnerException;
        }

        public static bool IsTypeVisible(Type type)
        {
            return IsTypeVisible(type, null);
        }

        public static bool IsTypeNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsTypeVisible(Type type, string friendlyAssemblyName)
        {
            if (type.IsVisible)
            {
                return true;
            }
            else
            {
                if (friendlyAssemblyName != null
                    && friendlyAssemblyName.Length > 0
                    && (!type.IsNested || type.IsNestedPublic ||
                     (!type.IsNestedPrivate && (type.IsNestedAssembly || type.IsNestedFamORAssem))))
                {
                    object[] attrs = type.Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false);
                    foreach (InternalsVisibleToAttribute ivta in attrs)
                    {
                        if (ivta.AssemblyName.Split(',')[0] == friendlyAssemblyName)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static Type[] GetInterfaces(Type type)
        {
            AssertUtils.ArgumentNotNull(type, "type");

            if (type.IsInterface)
            {
                List<Type> interfaces = new List<Type>();
                interfaces.Add(type);
                interfaces.AddRange(type.GetInterfaces());
                return interfaces.ToArray();
            }
            else
            {
                return type.GetInterfaces();
            }
        }

        public static Exception GetExplicitBaseException(Exception ex)
        {
            Exception innerEx = ex.InnerException;
            while (innerEx != null &&
                !(innerEx is NullReferenceException))
            {
                ex = innerEx;
                innerEx = innerEx.InnerException;
            }
            return ex;
        }

        public static object GetInstanceFieldValue(object obj, string fieldName)
        {
            if (obj == null)
                throw new ArgumentNullException("obj", "obj is null.");
            if (StringUtils.IsNullOrEmpty(fieldName))
                throw new ArgumentException("fieldName is null or empty.", "fieldName");

            FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null)
                return f.GetValue(obj);
            else
            {
                throw new ArgumentException(string.Format("Non-public instance field '{0}' could not be found in class of type '{1}'", fieldName, obj.GetType().ToString()));
            }
        }

        public static void SetInstanceFieldValue(object obj, string fieldName, object fieldValue)
        {

            if (obj == null)
                throw new ArgumentNullException("obj", "obj is null.");
            if (StringUtils.IsNullOrEmpty(fieldName))
                throw new ArgumentException("fieldName is null or empty.", "fieldName");
            if (fieldValue == null)
                throw new ArgumentNullException("fieldValue", "fieldValue is null.");

            FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (f != null)
            {
                if (f.FieldType != fieldValue.GetType())
                    throw new ArgumentException(string.Format("fieldValue for fieldName '{0}' of object type '{1}' must be of type '{2}' but was of type '{3}'", fieldName, obj.GetType().ToString(), f.FieldType.ToString(), fieldValue.GetType().ToString()), "fieldValue");

                f.SetValue(obj, fieldValue);
            }
            else
            {
                throw new ArgumentException(string.Format("Non-public instance field '{0}' could not be found in class of type '{1}'", fieldName, obj.GetType().ToString()));
            }
        }


        private delegate void MemberwiseCopyHandler(object a, object b);

        private static readonly Dictionary<Type, MemberwiseCopyHandler> s_handlerCache = new Dictionary<Type, MemberwiseCopyHandler>();

        private const BindingFlags FIELDBINDINGS =
            BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, FieldInfo[]> s_fieldCache = new Dictionary<Type, FieldInfo[]>();

        private static FieldInfo[] GetFields(Type type)
        {
            lock (s_fieldCache)
            {
                FieldInfo[] fields;
                if (!s_fieldCache.TryGetValue(type, out fields))
                {
                    List<FieldInfo> fieldList = new List<FieldInfo>();
                    CollectFieldsRecursive(type, fieldList);
                    fields = fieldList.ToArray();
                    s_fieldCache[type] = fields;
                }
                return fields;
            }
        }

        private static void CollectFieldsRecursive(Type type, List<FieldInfo> fieldList)
        {
            if (type == typeof(object))
                return;

            FieldInfo[] fields = type.GetFields(FIELDBINDINGS);
            fieldList.AddRange(fields);
            CollectFieldsRecursive(type.BaseType, fieldList);
        }

        private readonly static Regex methodMatchRegex = new Regex(
            @"(?<methodName>([\w]+\.)*[\w\*]+)(?<parameters>(\((?<parameterTypes>[\w\.]+(,[\w\.]+)*)*\))?)",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static bool MethodMatch(string pattern, MethodInfo method)
        {
            Match m = methodMatchRegex.Match(pattern);

            if (!m.Success)
                throw new ArgumentException(String.Format("The pattern [{0}] is not well-formed.", pattern));

            // 检测方法名称
            string methodNamePattern = m.Groups["methodName"].Value;
            if (!MatchUtils.SimpleMatch(methodNamePattern, method.Name))
            {
                return false;
            }

            if (m.Groups["parameters"].Value.Length > 0)
            {
                // 检测参数类型
                string parameters = m.Groups["parameterTypes"].Value;
                string[] paramTypes =
                    (parameters.Length == 0)
                    ? new string[0]
                    : StringUtils.DelimitedListToStringArray(parameters, ",");
                ParameterInfo[] paramInfos = method.GetParameters();

                // 验证参数个数
                if (paramTypes.Length != paramInfos.Length)
                {
                    return false;
                }

                //// 匹配参数类型
                //for (int i = 0; i < paramInfos.Length; i++)
                //{
                //    if (paramInfos[i].ParameterType != TypeResolutionUtils.ResolveType(paramTypes[i]))
                //        return false;
                //}
            }

            return true;
        }
    }
}
