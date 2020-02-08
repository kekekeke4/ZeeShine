using System;
using System.Collections;
using System.Text;

namespace ZeeShine.Utils
{
    public sealed class ArrayUtils
    {
        public static bool HasElements(ICollection collection)
        {
            if (!HasLength(collection)) return false;
            IEnumerator it = collection.GetEnumerator();
            while(it.MoveNext())
            {
                if (it.Current == null ) return false;
            }
            return true;
        }

        
        public static bool HasLength(ICollection collection)
        {
            return !( (collection == null) || (collection.Count == 0) );
        }

        public static bool AreEqual(Array a, Array b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a != null && b != null)
            {
                if (a.Length == b.Length)
                {
                    for (int i = 0; i < a.Length; i++)
                    {
                        object elemA = a.GetValue(i);
                        object elemB = b.GetValue(i);
                        
                        if (elemA is Array && elemB is Array)
                        {
                            if (!AreEqual(elemA as Array, elemB as Array))
                            {
                                return false;
                            }
                        }
                        else if (!Equals(elemA, elemB))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static int GetHashCode(Array array)
        {
            int hashCode = 0;

            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    object el = array.GetValue(i);
                    if (el != null)
                    {
                        if (el is Array)
                        {
                            hashCode += 17 * GetHashCode(el as Array);
                        }
                        else
                        {
                            hashCode += 13 * el.GetHashCode();
                        }
                    }
                }
            }

            return hashCode;
        }

        public static string ToString(Array array)
        {
            if (array == null)
            {
                return "null";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < array.Length; i++)
            {
                object val = array.GetValue(i);
                sb.Append(val == null ? "null" : val.ToString());
                
                if (i < array.Length - 1)
                {
                    sb.Append(", ");    
                }
            }

            sb.Append('}');

            return sb.ToString();
        }

        public static Array Concat(Array first, Array second)
        {
            if (first == null) return second;
            if (second == null) return first;

            Type resultElementType;
            Type firstElementType = first.GetType().GetElementType();
            Type secondElementType = second.GetType().GetElementType();
            if (firstElementType.IsAssignableFrom(secondElementType))
            {
                resultElementType = firstElementType;
            }
            else if (secondElementType.IsAssignableFrom(firstElementType))
            {
                resultElementType = secondElementType;
            }
            else
            {
                throw new ArgumentException(string.Format("Array element types '{0}' and '{1}' are not compatible", firstElementType, secondElementType));
            }
            Array result = Array.CreateInstance(resultElementType, first.Length + second.Length);
            Array.Copy( first, result, first.Length );
            Array.Copy(second, 0, result, first.Length, second.Length);
            return result;
        }
    }
}