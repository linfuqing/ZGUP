using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace ZG
{
    public static class GuidUtility
    {
        public static int IndexOf(this IEnumerable<Guid> guids, Guid guid)
        {
            if (guids == null)
                return -1;

            int index = 0;
            foreach (Guid temp in guids)
            {
                if (temp == guid)
                    return index;

                ++index;
            }

            return -1;
        }

        public static int IndexOf(this IEnumerable<string> guids, Guid guid)
        {
            if (guids == null)
                return -1;

            int index = 0;
            foreach (string temp in guids)
            {
                if (new Guid(temp) == guid)
                    return index;

                ++index;
            }

            return -1;
        }

        public static Guid GetGuid(this object target)
        {
            Guid result = Guid.Empty;
            Type type = target == null ? null : target.GetType();
            if (type != null)
            {
                PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfos != null)
                {
                    MethodInfo methodInfo;
                    foreach (PropertyInfo propertyInfo in propertyInfos)
                    {
                        methodInfo = propertyInfo == null ? null : propertyInfo.GetGetMethod();
                        if (methodInfo != null && methodInfo.ReturnType == typeof(Guid))
                        {
                            try
                            {
                                result = (Guid)methodInfo.Invoke(target, null);

                                break;
                            }
                            catch (Exception e)
                            {
                                e = e.InnerException;
                                if(e != null)
                                    UnityEngine.Debug.Log(e.Message);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static IEnumerable<Guid> GetGuids(this IEnumerable enumerable)
        {
            if (enumerable == null)
                yield break;

            int index = 0;
            foreach (object target in enumerable)
            {
                yield return GetGuid(target);

                ++index;
            }
        }
        
        public static object As(this string text, Type type, IEnumerable<Guid> guids)
        {
            if (type == typeof(string))
                return text;

            if (type == typeof(double))
                return string.IsNullOrEmpty(text) ? 0.0 : double.Parse(text);

            if (type == typeof(float))
                return string.IsNullOrEmpty(text) ? 0.0f : float.Parse(text);

            Guid guid;
            if (Guid.TryParse(text, out guid))
            {
                if (type == typeof(Guid))
                    return guid;

                if (type.IsIndex())
                {
                    int result;
                    if (guids == null)
                        result = string.IsNullOrEmpty(text) ? 0 : int.Parse(text);
                    else
                    {
                        result = -1;
                        int index = 0;
                        foreach (Guid temp in guids)
                        {
                            if (temp == guid)
                            {
                                result = index;

                                break;
                            }

                            ++index;
                        }
                    }

                    return Convert.ChangeType(result, type);
                }
            }
            
            return null;
        }
    }
}