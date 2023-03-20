using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public class ComponentManager<T> : MonoBehaviour where T : Component
    {
        private static Dictionary<string, T> __values;

        [SerializeField]
        internal T[] _values;

        public static T Find(string name)
        {
            return __values != null && __values.TryGetValue(name, out var value) ? value : default;
        }

        void OnEnable()
        {
            if (_values == null || _values.Length < 1)
                _values = GetComponents<T>();

            if (_values != null && _values.Length > 0)
            {
                if (__values == null)
                    __values = new Dictionary<string, T>();

                foreach (var value in _values)
                    __values.Add(value.name, value);
            }
            else
                Debug.LogError($"Empty {nameof(T)} Manager {name}", this);
        }

        void OnDisable()
        {
            if (_values != null)
            {
                foreach (var value in _values)
                    __values.Remove(value.name);
            }
        }
    }

    public class ComponentManager : ComponentManager<Component>
    {

    }
}