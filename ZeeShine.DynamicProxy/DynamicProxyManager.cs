using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace ZeeShine.DynamicProxy
{
    public sealed class DynamicProxyManager
    {
        public const string ASSEMBLY_NAME = "ZeeShine.DynamicProxy";

        private const TypeAttributes TYPE_ATTRIBUTES = TypeAttributes.BeforeFieldInit | TypeAttributes.Public;

        public static TypeBuilder CreateTypeBuilder(string typeName, Type baseType)
        {
            ModuleBuilder module = DynamicCodeManager.GetModuleBuilder(ASSEMBLY_NAME);

            try
            {
                if (baseType == null)
                {
                    return module.DefineType(typeName, TYPE_ATTRIBUTES);
                }
                else
                {
                    return module.DefineType(typeName, TYPE_ATTRIBUTES, baseType);
                }
            }
            catch (ArgumentException ex)
            {
                Type alreadyRegisteredType = module.GetType(typeName, true);

                string msg;
                
                if (alreadyRegisteredType != null)
                    msg = "Proxy already registered for \"{0}\" as Type \"{1}\".";
                else
                    msg = "Proxy already registered for \"{0}\".";

                throw new ArgumentException(string.Format(msg, typeName, alreadyRegisteredType.FullName), ex);
            }
        }

        //[Conditional("DEBUG_DYNAMIC")]
        //public static void SaveAssembly()
        //{
        //    DynamicCodeManager.SaveAssembly(ASSEMBLY_NAME);
        //}
    }
}