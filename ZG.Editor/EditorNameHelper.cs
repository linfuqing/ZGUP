using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public static class EditorNameHelper
    {
        private struct Key : IEquatable<Key>
        {
            public bool isShowIndex;
            public string nameKey;
            public IEnumerable enumerable;

            public bool Equals(Key other)
            {
                return isShowIndex = other.isShowIndex && nameKey == other.nameKey && enumerable == other.enumerable;
            }

            public override int GetHashCode()
            {
                return enumerable == null ? base.GetHashCode() : enumerable.GetHashCode();
            }
        }

        private struct Value
        {
            public long ticks;
            public string[] names;
        }

        private static Dictionary<Key, Value> __nameCaches;

        public static long timeout = TimeSpan.TicksPerSecond * 16;

        public static string GetPath(
            int pathLevel,
            string path,
            string propertyPath)
        {
            if (pathLevel < 0)
                propertyPath = path;
            else
            {
                if (propertyPath == null)
                    return null;

                int index = propertyPath.Length;
                for (int i = 0; i <= pathLevel; ++i)
                {
                    index = propertyPath.LastIndexOf('.', index - 1);
                    if (index == -1)
                        break;
                }

                propertyPath = propertyPath.Remove(index + 1);
                propertyPath += path;
            }

            return propertyPath;
        }

        public static string GetName(this SerializedProperty property, IEnumerable<string> names)
        {
            if (property == null)
                return null;

            SerializedPropertyType type = property.propertyType;
            if (type == SerializedPropertyType.String)
                return ObjectNames.NicifyVariableName(property.stringValue);

            if (type != SerializedPropertyType.Integer)
                return property.displayName;

            if (names == null)
                return property.displayName;
            else
            {
                int index = 0;
                int result = property.intValue;
                foreach (string targetName in names)
                {
                    if (index == result)
                        return targetName;

                    ++index;
                }
            }

            return property.displayName;
        }

        public static string[] GetNames(this object target, string nameKey, bool isShowIndex)
        {
            IEnumerable enumerable = target as IEnumerable;
            if (enumerable == null)
            {
                GameObject gameObject = target as GameObject;
                if (gameObject == null && target is Component)
                {
                    Component component = (Component)target;
                    gameObject = component == null ? null : component.gameObject;
                }

                if (gameObject != null)
                    enumerable = gameObject.transform;
            }

            if (enumerable == null)
                return null;

            long ticks = DateTime.UtcNow.Ticks;

            Key key;
            key.isShowIndex = isShowIndex;
            key.nameKey = nameKey;
            key.enumerable = enumerable;
            if (__nameCaches != null && __nameCaches.TryGetValue(key, out var value) && value.ticks + timeout > ticks)
                return value.names;

            //Type type = enumerable.GetType();

            bool isPrefab;
            int index = 0;
            string temp;
            Type type;
            UnityEngine.Object prefab;
            List<string> names = null;
            foreach (object element in enumerable)
            {
                isPrefab = false;

                type = element == null ? null : element.GetType();
                if (type != null)
                {
                    prefab = element as UnityEngine.Object;
                    try
                    {
                        isPrefab = prefab != null && (type == typeof(GameObject) || type.IsSubclassOf(typeof(Component)));
                        if (isPrefab)
                        {
                            switch (PrefabUtility.GetPrefabAssetType(prefab))
                            {
                                case PrefabAssetType.NotAPrefab:
                                    target = element;

                                    isPrefab = false;
                                    break;
                                case PrefabAssetType.MissingAsset:
                                    target = null;

                                    isPrefab = false;
                                    break;
                                default:
                                    target = PrefabUtility.InstantiatePrefab(prefab);
                                    break;
                            }
                        }
                        else
                            target = element;
                    }
                    catch (Exception e)
                    {
                        isPrefab = false;

                        target = null;

                        Debug.LogError(e.Message + ":" + e.StackTrace);
                    }
                }

                temp = string.IsNullOrEmpty(nameKey) ? null : target.GetName(nameKey);
                if (string.IsNullOrEmpty(temp))
                {
                    temp = target.GetName();
                    if (string.IsNullOrEmpty(temp))
                        temp = "Element " + index;
                    else if (isShowIndex)
                        temp = index.ToString() + '-' + temp;
                }
                else if (isShowIndex)
                    temp = index.ToString() + '-' + temp;

                if (isPrefab && target is UnityEngine.Object)
                    UnityEngine.Object.DestroyImmediate(PrefabUtility.GetNearestPrefabInstanceRoot(target as UnityEngine.Object));

                if (names == null)
                    names = new List<string>();

                names.Add(temp);

                ++index;
            }

            if (names == null)
                value.names = null;
            else
            {
                value.ticks = ticks;
                value.names = names.ToArray();

                if (__nameCaches == null)
                    __nameCaches = new Dictionary<Key, Value>();

                __nameCaches[key] = value;
            }

            return value.names;
        }

        public static string[] GetNames(
            this SerializedProperty property,
            string nameKey,
            string path,
            int pathLevel)
        {
            SerializedObject serializedObject = property == null ? null : property.serializedObject;
            if (serializedObject == null)
                return null;

            serializedObject.UpdateIfRequiredOrScript();

            UnityEngine.Object targetObject = serializedObject.targetObject;
            if (targetObject == null)
                return null;

            string propertyPath = GetPath(pathLevel, path, EditorHelper.GetPropertyPath(property.propertyPath));
            if (propertyPath == null)
                return null;

            return GetNames(targetObject.Get(ref propertyPath), nameKey, property.propertyType == SerializedPropertyType.Integer);
        }
    }
}