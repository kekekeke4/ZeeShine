using System.Reflection;
using System.Reflection.Emit;

namespace ZeeShine.DynamicProxy
{
    public interface IProxyMethodBuilder
    {
        MethodBuilder BuildProxyMethod(MethodInfo method, MethodInfo intfMethod);
    }
}
