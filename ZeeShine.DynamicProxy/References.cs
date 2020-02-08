using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using ZeeShine.Utils;

namespace ZeeShine.DynamicProxy
{
    internal struct References
    {
        // 字段
        public static readonly FieldInfo AdvisedField =
            typeof(AdvisedProxy).GetField("advised", BindingFlags.Instance | BindingFlags.Public);

        public static readonly FieldInfo TargetTypeField =
            typeof(AdvisedProxy).GetField("targetType", BindingFlags.Instance | BindingFlags.Public);

        public static readonly FieldInfo IntroductionsField =
            typeof(AdvisedProxy).GetField("m_introductions", BindingFlags.Instance | BindingFlags.Public);

        public static readonly FieldInfo TargetSourceField =
            typeof(AdvisedProxy).GetField("m_targetSource", BindingFlags.Instance | BindingFlags.Public);

        public static readonly ConstructorInfo AdvisedProxyConstructor =
            typeof(AdvisedProxy).GetConstructor(new Type[] { typeof(IAdvised)});

        public static readonly ConstructorInfo AdvisedProxySerializationConstructor =
            typeof(AdvisedProxy).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                                                 new Type[] { typeof(SerializationInfo), typeof(StreamingContext) },
                                                 null);
        public static readonly ConstructorInfo ObjectConstructor =
            typeof(Object).GetConstructor(Type.EmptyTypes);

        // 方法
        public static readonly MethodInfo PushProxyMethod =
            typeof(DynamicContext).GetMethod("PushProxy", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Object) }, null);

        public static readonly MethodInfo PopProxyMethod =
            typeof(DynamicContext).GetMethod("PopProxy", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);

        public static readonly MethodInfo InvokeMethod =
            typeof(AdvisedProxy).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Object), typeof(Object), typeof(Type), typeof(MethodInfo), typeof(MethodInfo), typeof(Object[]), typeof(IList) }, null);

        public static readonly MethodInfo GetInterceptorsMethod =
            typeof(AdvisedProxy).GetMethod("GetInterceptors", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(MethodInfo) }, null);

        public static readonly MethodInfo GetTargetMethod =
            typeof(AdvisedProxy).GetMethod("GetTarget", Type.EmptyTypes);

        public static readonly MethodInfo ReleaseTargetMethod =
            typeof(AdvisedProxy).GetMethod("ReleaseTarget", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Object) }, null);

        public static readonly MethodInfo GetTypeMethod =
            typeof(Object).GetMethod("GetType", Type.EmptyTypes);

        public static readonly MethodInfo GetTypeFromHandle =
            typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });

        public static readonly MethodInfo MakeGenericMethod =
            typeof(MethodInfo).GetMethod("MakeGenericMethod", new Type[] { typeof(Type[]) });

        public static readonly MethodInfo DisposeMethod =
            typeof(IDisposable).GetMethod("Dispose", Type.EmptyTypes);

        public static readonly MethodInfo AddSerializationValue =
            typeof(SerializationInfo).GetMethod("AddValue", new Type[] { typeof(string), typeof(object) });

        public static readonly MethodInfo GetSerializationValue =
            typeof(SerializationInfo).GetMethod("GetValue", new Type[] { typeof(string), typeof(Type) });

        // 属性
        public static readonly MethodInfo ExposeProxyProperty =
            typeof(IAdvised).GetProperty("ExposeProxy", typeof(Boolean)).GetGetMethod();

        public static readonly MethodInfo CountProperty =
            typeof(ICollection).GetProperty("Count", typeof(Int32)).GetGetMethod();


        // 方法
        public static readonly MethodInfo GetTypeFromHandleMethod =
            typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });

        public static readonly MethodInfo UnderstandsMethod =
            typeof(AssertUtils).GetMethod("Understands", new Type[] { typeof(object), typeof(string), typeof(Type) });
    }
}
