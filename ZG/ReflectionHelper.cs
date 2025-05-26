using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace ZG
{
    public static class ReflectionHelper
    {
        public struct EqualityComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        public static bool IsIndex(this Type type)
        {
            return type == typeof(int) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(ushort) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(long);
        }

        public static bool IsGenericTypeOf(this Type type, Type definition, out Type genericType)
        {
            if (type == null || definition == null || !definition.IsGenericTypeDefinition)
            {
                genericType = null;

                return false;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
            {
                genericType = type;

                return true;
            }

            return type.BaseType.IsGenericTypeOf(definition, out genericType);
        }

        public static Type GetTypeIsDefined<T>(this Type type, Type baseType = null) where T : Attribute
        {
            if (type == null || type == baseType)
                return null;

            if (type.IsDefined(typeof(T), false))
                return type;

            return GetTypeIsDefined<T>(type.BaseType, baseType);
        }

        public static Type GetArrayElementType(this Type type)
        {
            if (type == null)
                return null;

            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
            {
                Type[] genericArguments = type.GetGenericArguments();
                if (genericArguments != null && genericArguments.Length > 0)
                    return genericArguments[0];

                return null;
            }

            return typeof(object);
        }

        public static FieldInfo GetInheritedField(this Type type, string name, BindingFlags bindingFlags)
        {
            if (type == null)
                return null;

            FieldInfo fieldInfo = type.GetField(name, bindingFlags);
            return fieldInfo == null ? GetInheritedField(type.BaseType, name, bindingFlags) : fieldInfo;
        }

        public static object Get(
            this object root, 
            Action<object, FieldInfo> visit, 
            string path, 
            int count, 
            ref int startIndex, 
            out int index, 
            out FieldInfo fieldInfo, 
            out object parent)
        {
            index = -1;
            fieldInfo = null;
            parent = null;

            Type type;
            IList list;
            object result = root;
            string substring;
            char temp;
            int i, length, endIndex, pathLength = Math.Min(path == null ? 0 : path.Length, startIndex + count);
            while (startIndex < pathLength)
            {
                if (result == null)
                    return null;

                endIndex = path.IndexOf('.', startIndex);
                endIndex = endIndex == -1 ? pathLength : endIndex;
                length = endIndex - startIndex;

                i = path.IndexOf('[', startIndex, length);
                if (i != -1)
                {
                    substring = path.Substring(startIndex, i - startIndex);

                    index = 0;
                    while (++i < endIndex)
                    {
                        temp = path[i];
                        if (char.IsNumber(temp))
                        {
                            index *= 10;
                            index += Convert.ToInt32(temp) - 48;
                        }
                        else if (temp == ']')
                            break;
                    }
                }
                else
                {
                    substring = path.Substring(startIndex, length);

                    index = -1;
                }

                type = result.GetType();
                
                fieldInfo = type.GetInheritedField(substring, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo == null)
                    return null;

                if (visit != null)
                    visit(result, fieldInfo);

                parent = result;
                result = fieldInfo.GetValue(result);

                if (index != -1)
                {
                    list = result as IList;
                    if (list == null)
                        return null;

                    fieldInfo = null;

                    parent = list;

                    if (index < list.Count)
                        result = list[index];
                    else
                        return null;
                }

                startIndex = endIndex + 1;
            }

            return result;
        }

        public static object Get(this object root, Action<object, FieldInfo> visit, ref string path, out FieldInfo fieldInfo, out object parent)
        {
            int count = path == null ? 0 : path.Length, startIndex = 0;
            object target = root.Get(visit, path, count, ref startIndex, out _, out fieldInfo, out parent);
            if (startIndex < count)
                path = path.Substring(startIndex);

            return target;
        }

        public static object Get(this object root, ref string path, out FieldInfo fieldInfo, out object parent)
        {
            return root.Get(null, ref path, out fieldInfo, out parent);
        }

        public static object Get(this object root, ref string path)
        {
            return Get(root, ref path, out _, out _);
        }

        public static object Get(this object root, string path)
        {
            return Get(root, ref path);
        }

        public static void CopyTo(
            this object source,
            object destination,
            IEnumerable<Type> keepTypes,
            Predicate<FieldInfo> sourcePredicate,
            Predicate<FieldInfo> destinationPredicate,
            Dictionary<object, object> targets)
        {
            Type sourceType = source?.GetType();
            if (sourceType == null)
                return;

            Type destinationType = destination?.GetType();
            if (destinationType == null)
                return;

            bool isKeep;
            int count, i;
            IList sourceArray, destinationArray;
            Type sourceFieldType, destinationFieldType;
            object sourceValue, destinationValue;
            FieldInfo destinationFieldInfo;
            FieldInfo[] fieldInfos;
            do
            {
                fieldInfos = sourceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfos == null)
                    continue;

                foreach (FieldInfo sourceFieldInfo in fieldInfos)
                {
                    sourceFieldType = sourceFieldInfo?.FieldType;
                    if (sourceFieldType == null)
                        continue;

                    if (sourcePredicate != null && !sourcePredicate(sourceFieldInfo))
                        continue;

                    destinationFieldInfo = GetInheritedField(destinationType, sourceFieldInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (destinationFieldInfo == null)
                        continue;

                    if (destinationPredicate != null && !destinationPredicate(destinationFieldInfo))
                        continue;

                    sourceValue = sourceFieldInfo.GetValue(source);

                    if (sourceFieldType.IsPrimitive || sourceFieldType.IsAbstract || sourceFieldType == typeof(string) || sourceValue == null || sourceValue.GetType() == typeof(Pointer))
                    {
                        if (sourceFieldType != destinationFieldInfo.FieldType)
                            continue;

                        isKeep = true;
                    }
                    else
                    {
                        isKeep = false;
                        if (keepTypes != null)
                        {
                            foreach (Type keepType in keepTypes)
                            {
                                if (sourceFieldType == keepType || sourceFieldType.IsSubclassOf(keepType))
                                {
                                    isKeep = true;

                                    break;
                                }
                            }
                        }
                    }

                    try
                    {
                        if (isKeep)
                            destinationValue = sourceValue;
                        else
                        {
                            if (sourceFieldType.IsArray)
                            {
                                destinationFieldType = destinationFieldInfo.FieldType;
                                if (destinationFieldType == null || !destinationFieldType.IsArray)
                                    continue;

                                sourceArray = sourceValue as IList;
                                count = sourceArray == null ? 0 : sourceArray.Count;
                                if (count > 0)
                                {
                                    destinationFieldType = destinationFieldType.GetElementType();
                                    destinationArray = Array.CreateInstance(destinationFieldType, count);
                                    if (destinationArray != null)
                                    {
                                        sourceFieldType = sourceFieldType.GetElementType();
                                        if (sourceFieldType.IsPrimitive || sourceFieldType.IsAbstract || sourceFieldType == typeof(string))
                                        {
                                            if (sourceFieldType != destinationFieldType)
                                                continue;

                                            isKeep = true;
                                        }
                                        else
                                        {
                                            isKeep = false;
                                            if (keepTypes != null)
                                            {
                                                foreach (Type keepType in keepTypes)
                                                {
                                                    if (sourceFieldType == keepType || sourceFieldType.IsSubclassOf(keepType))
                                                    {
                                                        isKeep = true;

                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        if (isKeep)
                                        {
                                            for (i = 0; i < count; ++i)
                                                destinationArray[i] = sourceArray[i];
                                        }
                                        else
                                        {
                                            for (i = 0; i < count; ++i)
                                            {
                                                sourceValue = sourceArray[i];
                                                if (targets == null || !targets.TryGetValue(sourceValue, out destinationValue) || destinationValue == null)
                                                {
                                                    destinationValue = Activator.CreateInstance(destinationFieldType);

                                                    if (targets == null)
                                                        targets = new Dictionary<object, object>();

                                                    targets.Add(sourceValue, destinationValue);

                                                    CopyTo(sourceArray[i], destinationValue, keepTypes, sourcePredicate, destinationPredicate, targets);
                                                }

                                                destinationArray[i] = destinationValue;
                                            }
                                        }
                                    }

                                    destinationValue = destinationArray;
                                }
                                else
                                    destinationValue = null;
                            }
                            else
                            {
                                if (targets == null || !targets.TryGetValue(sourceValue, out destinationValue) || destinationValue == null)
                                {
                                    destinationValue = Activator.CreateInstance(destinationFieldInfo.FieldType);

                                    if (targets == null)
                                        targets = new Dictionary<object, object>();

                                    targets.Add(sourceValue, destinationValue);

                                    CopyTo(sourceValue, destinationValue, keepTypes, sourcePredicate, destinationPredicate, targets);
                                }
                            }
                        }

                        destinationFieldInfo.SetValue(destination, destinationValue);
                    }
                    catch (Exception exception)
                    {
                        if (exception != null)
                            UnityEngine.Debug.LogException(exception.InnerException ?? exception);
                    }
                }

                sourceType = sourceType.BaseType;
            } while (sourceType != null);
        }

        public static bool IsDependOn<T>(
            this T instance,
            object target,
            IEqualityComparer equalityComparer,
            IEnumerable<Type> keepTypes,
            ref HashSet<object> targets)
        {
            if (target == null)
                return false;

            if (targets == null)
                targets = new HashSet<object>(new EqualityComparer());

            if (!targets.Add(target))
                return false;

            Type type = target.GetType();
            if (type == null)
                return false;

            bool isKeep;
            int count, i;
            IList array;
            Type fieldType;
            object value;
            FieldInfo[] fieldInfos;
            do
            {
                fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfos == null)
                    continue;

                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    fieldType = fieldInfo?.FieldType;
                    if (fieldType == null)
                        continue;

                    value = fieldInfo.GetValue(target);
                    if (typeof(T) == fieldType || typeof(T).IsSubclassOf(fieldType))
                    {
                        try
                        {
                            if (equalityComparer == null ? instance.Equals(value) : equalityComparer.Equals(instance, value))
                                return true;
                        }
                        catch (Exception exception)
                        {
                            if (exception != null)
                                UnityEngine.Debug.LogException(exception.InnerException ?? exception);
                        }
                    }
                    else if (value != null)
                    {
                        if (fieldType.IsArray)
                        {
                            fieldType = fieldType.GetElementType();

                            isKeep = false;
                            if (keepTypes != null)
                            {
                                foreach (Type keepType in keepTypes)
                                {
                                    if (keepType == fieldType || fieldType.IsSubclassOf(keepType))
                                    {
                                        isKeep = true;

                                        break;
                                    }
                                }
                            }

                            if (!isKeep)
                            {
                                array = value as IList;
                                count = array == null ? 0 : array.Count;
                                if (count > 0)
                                {
                                    for (i = 0; i < count; ++i)
                                    {
                                        if (IsDependOn(instance, array[i], equalityComparer, keepTypes, ref targets))
                                            return true;
                                    }
                                }
                            }
                        }
                        else if (!fieldType.IsPrimitive && fieldType != typeof(string) && value.GetType() != typeof(Pointer))
                        {
                            isKeep = false;
                            if (keepTypes != null)
                            {
                                foreach (Type keepType in keepTypes)
                                {
                                    if (keepType == fieldType || fieldType.IsSubclassOf(keepType))
                                    {
                                        isKeep = true;

                                        break;
                                    }
                                }
                            }

                            if (!isKeep && IsDependOn(instance, value, equalityComparer, keepTypes, ref targets))
                                return true;
                        }
                    }
                }

                type = type.BaseType;
            } while (type != null);

            return false;
        }

        public static void ChangeDependencies(
            this object target,
            object source,
            object destination,
            IEqualityComparer equalityComparer,
            IEnumerable<Type> keepTypes,
            ref HashSet<object> targets)
        {
            if (target == null)
                return;

            if (targets == null)
                targets = new HashSet<object>(new EqualityComparer());

            if (!targets.Add(target))
                return;

            Type targetType = target.GetType();
            if (targetType == null)
                return;

            Type destinationType = destination.GetType();

            bool isKeep;
            int count, i;
            IList array;
            Type fieldType;
            object value;
            FieldInfo[] fieldInfos;
            do
            {
                fieldInfos = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfos == null)
                    continue;

                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    fieldType = fieldInfo?.FieldType;
                    if (fieldType == null)
                        continue;

                    value = fieldInfo.GetValue(target);
                    if (destinationType == fieldType || destinationType.IsSubclassOf(fieldType))
                    {
                        try
                        {
                            if (equalityComparer == null ? source.Equals(value) : equalityComparer.Equals(source, value))
                                fieldInfo.SetValue(target, destination);
                        }
                        catch (Exception exception)
                        {
                            if (exception != null)
                                UnityEngine.Debug.LogException(exception.InnerException ?? exception);
                        }
                    }
                    else if (value != null)
                    {
                        if (fieldType.IsArray)
                        {
                            fieldType = fieldType.GetElementType();

                            isKeep = false;
                            if (keepTypes != null)
                            {
                                foreach (Type keepType in keepTypes)
                                {
                                    if (keepType == fieldType || fieldType.IsSubclassOf(keepType))
                                    {
                                        isKeep = true;

                                        break;
                                    }
                                }
                            }

                            if (!isKeep)
                            {
                                array = value as IList;
                                count = array == null ? 0 : array.Count;
                                if (count > 0)
                                {
                                    for (i = 0; i < count; ++i)
                                        ChangeDependencies(array[i], source, destination, equalityComparer, keepTypes, ref targets);
                                }
                            }
                        }
                        else if (!fieldType.IsPrimitive && fieldType != typeof(string) && value.GetType() != typeof(Pointer))
                        {
                            isKeep = false;
                            if (keepTypes != null)
                            {
                                foreach (Type keepType in keepTypes)
                                {
                                    if (keepType == fieldType || fieldType.IsSubclassOf(keepType))
                                    {
                                        isKeep = true;

                                        break;
                                    }
                                }
                            }

                            if (!isKeep)
                                ChangeDependencies(value, source, destination, equalityComparer, keepTypes, ref targets);
                        }
                    }
                }

                targetType = targetType.BaseType;
            } while (targetType != null);
        }

        public static IEnumerable<KeyValuePair<object, FieldInfo>> GetDependencies(
            this object instance,
            object target,
            IEqualityComparer equalityComparer,
            IEnumerable<Type> keepTypes,
            HashSet<object> targets)
        {
            if (target == null)
                yield break;

            if (targets == null)
                targets = new HashSet<object>(new EqualityComparer());

            if (!targets.Add(target))
                yield break;

            Type targetType = target.GetType();
            if (targetType == null)
                yield break;

            Type type = instance.GetType();
            if (type == null)
                yield break;

            bool isKeep;
            int count, i;
            IList array;
            Type fieldType;
            object value;
            FieldInfo[] fieldInfos;
            IEnumerable<KeyValuePair<object, FieldInfo>> results;
            do
            {
                fieldInfos = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfos == null)
                    continue;

                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    fieldType = fieldInfo?.FieldType;
                    if (fieldType == null)
                        continue;

                    value = fieldInfo.GetValue(target);
                    if (type == fieldType || type.IsSubclassOf(fieldType))
                    {
                        if (equalityComparer == null ? instance.Equals(value) : equalityComparer.Equals(instance, value))
                            yield return new KeyValuePair<object, FieldInfo>(target, fieldInfo);
                    }
                    else if (value != null)
                    {
                        if (fieldType.IsArray)
                        {
                            fieldType = fieldType.GetElementType();

                            isKeep = false;
                            if (keepTypes != null)
                            {
                                foreach (Type keepType in keepTypes)
                                {
                                    if (keepType == fieldType || fieldType.IsSubclassOf(keepType))
                                    {
                                        isKeep = true;

                                        break;
                                    }
                                }
                            }

                            if (!isKeep)
                            {
                                array = value as IList;
                                count = array == null ? 0 : array.Count;
                                if (count > 0)
                                {
                                    for (i = 0; i < count; ++i)
                                    {
                                        results = GetDependencies(instance, array[i], equalityComparer, keepTypes, targets);
                                        if (results != null)
                                        {
                                            foreach (var result in results)
                                                yield return result;
                                        }
                                    }
                                }
                            }
                        }
                        else if (!fieldType.IsPrimitive && fieldType != typeof(string) && value.GetType() != typeof(Pointer))
                        {
                            isKeep = false;
                            if (keepTypes != null)
                            {
                                foreach (Type keepType in keepTypes)
                                {
                                    if (keepType == fieldType || fieldType.IsSubclassOf(keepType))
                                    {
                                        isKeep = true;

                                        break;
                                    }
                                }
                            }

                            if (!isKeep)
                            {
                                results = GetDependencies(instance, value, equalityComparer, keepTypes, targets);
                                if (results != null)
                                {
                                    foreach (var result in results)
                                        yield return result;
                                }
                            }
                        }
                    }
                }

                targetType = targetType.BaseType;
            } while (targetType != null);
        }
    }
}
