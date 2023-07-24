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

                foreach (var value in _values)
                    __values.Add(value.name, value);
            }

            if(_instances != null && _instances.Count > 0)
            {
                if (__values == null)
                    __values = new Dictionary<string, UnityEngine.Object>();

                foreach (var pair in _instances)
                    __values.Add(pair.Key, pair.Value);
            }
        }

        protected void OnDisable()
        {
            if (_values != null)
            {
                foreach (var value in _values)
                    __values.Remove(value.name);
            }

            if (_instances != null)
            {
                foreach (var pair in _instances)
                    __values.Remove(pair.Key);
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