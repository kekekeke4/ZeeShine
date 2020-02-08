using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Collections;

namespace ZeeShine.Utils
{
    public sealed class AssertUtils
    {
        public static void Understands(object target, string targetName, MethodBase method)
        {
            ArgumentNotNull(method, "method");

            if (target == null)
            {
                if (method.IsStatic)
                {
                    return;
                }
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Target '{0}' is null and target method '{1}.{2}' is not static.", targetName, method.DeclaringType.FullName, method.Name));
            }

            Understands(target, targetName, method.DeclaringType);
        }


        public static void Understands(object target, string targetName, Type requiredType)
        {
            ArgumentNotNull(requiredType, "requiredType");

            if (target == null)
            {
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Target '{0}' is null.", targetName));
            }

            //Type targetType;
            //if (RemotingServices.IsTransparentProxy(target))
            //{
            //    RealProxy rp = RemotingServices.GetRealProxy(target);
            //    IRemotingTypeInfo rti = rp as IRemotingTypeInfo;
            //    if (rti != null)
            //    {
            //        if (rti.CanCastTo(requiredType, target))
            //        {
            //            return;
            //        }
            //        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Target '{0}' is a transparent proxy that does not support methods of '{1}'.", targetName, requiredType.FullName));
            //    }
            //    targetType = rp.GetProxiedType();
            //}
            //else
            //{
            //    targetType = target.GetType();
            //}

            //if (!requiredType.IsAssignableFrom(targetType))
            //{
            //    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Target '{0}' of type '{1}' does not support methods of '{2}'.", targetName, targetType, requiredType.FullName));
            //}
        }

        public static void ArgumentNotNull(object argument, string name)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(
                    name,
                    string.Format(
                        CultureInfo.InvariantCulture,
                    "Argument '{0}' cannot be null.", name));
            }
        }

        public static void ArgumentNotNull(object argument, string name, string message)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(name, message);
            }
        }

        public static void ArgumentHasText(string argument, string name)
        {
            if (StringUtils.IsNullOrEmpty(argument))
            {
                throw new ArgumentNullException(
                    name,
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "Argument '{0}' cannot be null or resolve to an empty string : '{1}'.", name, argument));
            }
        }

        public static void ArgumentHasText(string argument, string name, string message)
        {
            if (StringUtils.IsNullOrEmpty(argument))
            {
                throw new ArgumentNullException(name, message);
            }
        }

        public static void ArgumentHasLength(ICollection argument, string name)
        {
            if (!ArrayUtils.HasLength(argument))
            {
                throw new ArgumentNullException(
                    name,
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "Argument '{0}' cannot be null or resolve to an empty array", name));
            }
        }

        public static void ArgumentHasLength(ICollection argument, string name, string message)
        {
            if (!ArrayUtils.HasLength(argument))
            {
                throw new ArgumentNullException(name, message);
            }
        }

        public static void ArgumentHasElements(ICollection argument, string name)
        {
            if (!ArrayUtils.HasElements(argument))
            {
                throw new ArgumentException(
                    name,
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "Argument '{0}' must not be null or resolve to an empty collection and must contain non-null elements", name));
            }
        }

        public static void AssertArgumentType(object argument, string argumentName, Type requiredType, string message)
        {
            if (argument != null && requiredType != null && !requiredType.IsAssignableFrom(argument.GetType()))
            {
                throw new ArgumentException(message, argumentName);
            }
        }

        public static void IsTrue(bool expression, string message)
        {
            if (!expression)
            {
                throw new ArgumentException(message);
            }
        }

        public static void IsTrue(bool expression)
        {
            IsTrue(expression, "[Assertion failed] - this expression must be true");
        }

        public static void State(bool expression, string message)
        {
            if (!expression)
            {
                throw new InvalidOperationException(message);
            }
        }

        private AssertUtils()
        {
        }
    }
}
