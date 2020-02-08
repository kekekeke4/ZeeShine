using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace ZeeShine.DynamicProxy
{
    public class TargetProxyMethodBuilder : AbstractProxyMethodBuilder 
    {
        protected LocalBuilder target;

        public TargetProxyMethodBuilder(TypeBuilder typeBuilder, 
            IProxyTypeBuilder proxyTypeBuilder, bool explicitImplementation, IDictionary targetMethods)
            : base(typeBuilder, proxyTypeBuilder, explicitImplementation, targetMethods)
        {
        }

        protected override void DeclareLocals(ILGenerator il, MethodInfo method)
        {
            base.DeclareLocals(il, method);
            target = il.DeclareLocal(typeof(object));

//#if DEBUG
//            target.SetLocalSymInfo("target");
//#endif
        }

        protected override void PushTarget(ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc, target);
        }

        protected override void GenerateMethodLogic(
            ILGenerator il, MethodInfo method, MethodInfo interfaceMethod)
        {
            PushAdvisedProxy(il);
            //il.Emit(OpCodes.Ldfld, References.TargetSourceField);  // kez ×¢ÊÍ
            //il.EmitCall(OpCodes.Callvirt, References.GetTargetMethod, null);  // kez ×¢ÊÍ
            il.EmitCall(OpCodes.Call, References.GetTargetMethod, null);  // kez Ìí¼Ó
            il.Emit(OpCodes.Stloc, target);

            base.GenerateMethodLogic(il, method, interfaceMethod);

            PushAdvisedProxy(il);
            //il.Emit(OpCodes.Ldfld, References.TargetSourceField);  // kez ×¢ÊÍ
            PushTarget(il);
            il.EmitCall(OpCodes.Callvirt, References.ReleaseTargetMethod/*GetReleaseTargetMethod*/, null);  // kez ×¢ÊÍ
        }

       
        protected override void CallDirectProxiedMethod(
            ILGenerator il, MethodInfo method, MethodInfo interfaceMethod)
        {
            if (interfaceMethod != null)
            {
                CallDirectTargetMethod(il, interfaceMethod);
            }
            else
            {
                CallDirectTargetMethod(il, method);
            }
        }
    }
}
