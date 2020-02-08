using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ZeeShine.Utils
{
    public sealed class AsyncUtils
    {
        public static bool IsAsyncMethod(MethodInfo method)
        {
            return method.ReturnType == typeof(Task) ||
                (method.ReturnType.IsGenericType &&
                 method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }

        public static async Task<T> AwaitTaskAndConvertResult<T, F>(Task<F> actualFromValue, Func<object, Task<object>> convertFunc, Func<Exception, bool> exceptionFunc, Action finalAction)
        {
            try
            {
                var f = await actualFromValue;
                if (convertFunc != null)
                {
                    var result = await convertFunc(f);
                    return (T)result;
                }
                return default(T);
            }
            catch (Exception exp)
            {
                if (exceptionFunc?.Invoke(exp) == true)
                {
                    throw exp;
                }
                return default(T);
            }
            finally
            {
                finalAction?.Invoke();
            }
        }

        public static async Task AwaitTask(Task actualReturnValue, Func<Exception, bool> exceptionFunc, Action finalAction)
        {
            try
            {
                await actualReturnValue;
            }
            catch (Exception exp)
            {
                if (exceptionFunc?.Invoke(exp) == true)
                {
                    throw exp;
                }
            }
            finally
            {
                finalAction?.Invoke();
            }
        }

        public static async Task AwaitTask<T>(Task<T> actualReturnValue,Action<T> action, Func<Exception, bool> exceptionFunc, Action finalAction)
        {
            try
            {
                var value = await actualReturnValue;
                action?.Invoke(value);
            }
            catch (Exception exp)
            {
                if (exceptionFunc?.Invoke(exp) == true)
                {
                    throw exp;
                }
            }
            finally
            {
                finalAction?.Invoke();
            }
        }

        public static async Task<T> AwaitTaskAndGetResult<T>(Task<T> actualReturnValue, Func<Exception, bool> exceptionFunc, Action finalAction)
        {
            try
            {
                return await actualReturnValue;
            }
            catch (Exception exp)
            {
                if (exceptionFunc?.Invoke(exp) == true)
                {
                    throw exp;
                }
                return default(T);
            }
            finally
            {
                finalAction?.Invoke();
            }
        }

        public static object AwaitTaskAndGetResultByType(Type taskReturnType, object actualReturnValue, Func<Exception, bool> exceptionFunc, Action finalAction)
        {
            return typeof(AsyncUtils)
                .GetMethod("AwaitTaskAndGetResult", BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod(taskReturnType)
                .Invoke(null, new object[] { actualReturnValue, exceptionFunc, finalAction });
        }

        public static object AwaitTaskAndConvertResultByType(Type taskReturnType, Type taskFromType, object actualFromValue, Func<object, Task<object>> convertFunc, Func<Exception, bool> exceptionFunc, Action finalAction)
        {
            return typeof(AsyncUtils)
              .GetMethod("AwaitTaskAndConvertResult", BindingFlags.Public | BindingFlags.Static)
              .MakeGenericMethod(taskReturnType, taskFromType)
              .Invoke(null, new object[] { actualFromValue, convertFunc, exceptionFunc, finalAction });
        }
    }
}
