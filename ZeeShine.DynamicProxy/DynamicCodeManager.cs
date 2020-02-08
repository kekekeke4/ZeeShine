using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;

namespace ZeeShine.DynamicProxy
{
    public sealed class DynamicCodeManager
    {
        private static readonly ConcurrentDictionary<string, ModuleBuilder> s_moduleCache;

        static DynamicCodeManager()
        {
            s_moduleCache = new ConcurrentDictionary<string, ModuleBuilder>();
        }

        private DynamicCodeManager()
        {
            throw new InvalidOperationException();
        }

        public static ModuleBuilder GetModuleBuilder(string assemblyName)
        {
            if (!s_moduleCache.TryGetValue(assemblyName, out ModuleBuilder module))
            {
                AssemblyName an = new AssemblyName();
                an.Name = assemblyName;
                an.SetPublicKey(Assembly.GetExecutingAssembly().GetName().GetPublicKey());
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
                module = assembly.DefineDynamicModule(an.Name);
                //s_moduleCache.TryAdd(assemblyName, module);
                s_moduleCache.AddOrUpdate(assemblyName, module, (k, old) => module);
            }
            return module;
        }
        
        //[Conditional("DEBUG_DYNAMIC")]        
        //public static void SaveAssembly( string assemblyName )
        //{
        //    //AssertHelper.ArgumentHasText(assemblyName, "assemblyName");
            
        //    //ModuleBuilder module = null;
        //    //lock(s_moduleCache.SyncRoot)
        //    //{
        //    //    module = (ModuleBuilder) s_moduleCache[assemblyName];
        //    //}
            
        //    //if(module == null)
        //    //{
        //    //    throw new ArgumentException(string.Format("'{0}' is not a valid dynamic assembly name", assemblyName), "assemblyName");
        //    //}

        //    //AssemblyBuilder assembly = (AssemblyBuilder) module.Assembly;
        //    ////assembly.Save(assembly.GetName().Name + ".dll");        
        //}

        public static void Clear()
        {
            s_moduleCache.Clear();
        }
    }
}