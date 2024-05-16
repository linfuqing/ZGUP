using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public interface IComponentWrapper
    {
        UnityEngine.Object As(string key);
    }

    public class ComponentManager<T> : MonoBehaviour where T : Component
    {
        [Serializable]
        public class Instances : Map<UnityEngine.Object>
        {

        }

        private static Dictionary<string, UnityEngine.Object> __values;

        [SerializeField]
        internal T[] _values;

        [SerializeField, Map]
        internal Instances _instances;

        public static Action<string, T> onChanged;

        public static void Change(string key)
        {
            if (onChanged != null)
                onChanged(key, Find(key));
        }

        public static T Find(string key)
        {
            return __values != null && __values.TryGetValue(key, out var value) ? __As(key, value) : default;
        }

        protected void OnEnable()
        {
            if ((_values == null || _values.Length < 1) && (_instances == null || _instances.Count < 1))
                _values = GetComponents<T>();

            if (_values != null && _values.Length > 0)
            {
                if (__values == null)
                    __values = new Dictionary<string, UnityEngine.Object>();

                string key;
                foreach (var value in _values)
                {
                    key = value.name;

                    __values[key] = value;

                    if (onChanged != null)
                        onChanged(key, value);
                }
            }

            if(_instances != null && _instances.Count > 0)
            {
                if (__values == null)
                    __values = new Dictionary<string, UnityEngine.Object>();

                string key;
                UnityEngine.Object value;
                foreach (var pair in _instances)
                {
                    key = pair.Key;
                    value = pair.Value;

                    __values[key] = value;

                    if (onChanged != null)
                        onChanged(key, __As(key, value));
                }
            }
        }

        protected void OnDisable()
        {
            UnityEngine.Object target;
            if (_values != null)
            {
                string name;
                foreach (var value in _values)
                {
                    name = value.name;
                    if (__values.TryGetValue(name, out target) && 
                        target == value && 
                        __values.Remove(name))
                    {
                        if(onChanged != null)
                            onChanged(value.name, default);
                    }
                }
            }

            if (_instances != null)
            {
                string key;
                foreach (var pair in _instances)
                {
                    key = pair.Key;
                    if (__values.TryGetValue(key, out target) &&
                        target == pair.Value && 
                        __values.Remove(key))
                    {
                        if (onChanged != null)
                            onChanged(key, default);
                    }
                }
            }
        }

        private static T __As(string key, UnityEngine.Object target)
        {
            var wrapper = target as IComponentWrapper;
            return (wrapper == null ? target : wrapper.As(key)) as T;
        }
    }

    public class ComponentManager : ComponentManager<Component>
    {

    }
}