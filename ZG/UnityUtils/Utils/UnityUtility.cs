using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public static class UnityUtility
    {
        public struct EqualityComparer : System.Collections.IEqualityComparer
        {
            bool System.Collections.IEqualityComparer.Equals(object x, object y)
            {
                System.Type type = x?.GetType() ?? y?.GetType();
                if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                    return x == y;

                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                return obj == null ? 0 : obj.GetHashCode();
            }
        }

        public static IEnumerable<KeyValuePair<Component, KeyValuePair<object, System.Reflection.FieldInfo>>> GetDependencies(
            this object instance,
            GameObject gameObject,
            bool isIncludeChildren,
            HashSet<object> targets)
        {
            System.Type[] keepTypes = new[] { typeof(UnityEngine.Object) };

            EqualityComparer equalityComparer = new EqualityComparer();

            Component[] components = gameObject == null ? null : gameObject.GetComponents<Component>();
            if (components == null)
                yield break;

            IEnumerable<KeyValuePair<object, System.Reflection.FieldInfo>> results;
            foreach (Component component in components)
            {
                results = instance.GetDependencies(component, equalityComparer, keepTypes, targets);
                if (results != null)
                {
                    foreach (var result in results)
                        yield return new KeyValuePair<Component, KeyValuePair<object, System.Reflection.FieldInfo>>(component, result);
                }
            }

            if (isIncludeChildren)
            {
                Transform transform = gameObject.transform;
                if (transform != null)
                {
                    IEnumerable<KeyValuePair<Component, KeyValuePair<object, System.Reflection.FieldInfo>>> childResults;
                    foreach (Transform child in transform)
                    {
                        if (child == null)
                            continue;

                        childResults = GetDependencies(instance, child.gameObject, true, targets);
                        if (childResults != null)
                        {
                            foreach (var childResult in childResults)
                                yield return childResult;
                        }
                    }
                }
            }
        }

        public static void ChangeDependencies(
            this GameObject gameObject,
            object source,
            object destination,
            ref HashSet<object> targets)
        {
            System.Type[] keepTypes = new[] { typeof(UnityEngine.Object) };

            EqualityComparer equalityComparer = new EqualityComparer();

            ((object)gameObject).ChangeDependencies(source, destination, equalityComparer, keepTypes, ref targets);

            Component[] components = gameObject == null ? null : gameObject.GetComponents<Component>();
            if (components == null)
                return;

            foreach (Component component in components)
                component.ChangeDependencies(source, destination, equalityComparer, keepTypes, ref targets);
        }

        public static void CopyTo(this object source, object destination, System.Collections.Generic.Dictionary<object, object> targets = null)
        {
            if(targets == null)
                targets = new System.Collections.Generic.Dictionary<object, object>();

            source.CopyTo(destination, new[] { typeof(UnityEngine.Object) }, __Predicate, __Predicate, targets);

            ISerializationCallbackReceiver serializationCallbackReceiver;
            foreach(var target in targets.Values)
            {
                serializationCallbackReceiver = target as ISerializationCallbackReceiver;
                if (serializationCallbackReceiver != null)
                    serializationCallbackReceiver.OnAfterDeserialize();
            }
        }

        private static bool __Predicate(System.Reflection.FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                return false;

            System.Type type = fieldInfo.DeclaringType;
            if (type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                return true;

            return fieldInfo.IsPublic ? !fieldInfo.IsDefined(typeof(System.NonSerializedAttribute), true) : fieldInfo.IsDefined(typeof(SerializeField), true);
        }
    }
}