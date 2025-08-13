using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ZG
{
    [Serializable]
    public class Map<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        internal TKey[] _keys;
        [SerializeField]
        internal TValue[] _values;

        private Dictionary<TKey, TValue> __instance;

        private Func<TKey, IEnumerable<TKey>, TKey> __getUniqueKey;

        public int Count
        {
            get
            {
                return __instance == null ? 0 : __instance.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (__instance == null)
                    throw new ArgumentOutOfRangeException();

                return __instance[key];
            }

            set
            {
                if (__instance == null)
                    __instance = new Dictionary<TKey, TValue>();

                __instance[key] = value;
            }
        }

        public Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                return __instance == null ? null : __instance.Keys;
            }
        }

        public Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                return __instance == null ? null : __instance.Values;
            }
        }

        public Map(Func<TKey, IEnumerable<TKey>, TKey> getUniqueKey)
        {
            __getUniqueKey = getUniqueKey;
        }

        public Map(IEqualityComparer<TKey> comparer, Func<TKey, IEnumerable<TKey>, TKey> getUniqueKey) : this(getUniqueKey)
        {
            __instance = new Dictionary<TKey, TValue>(comparer);
        }

        public Map(int capacity, IEqualityComparer<TKey> comparer, Func<TKey, IEnumerable<TKey>, TKey> getUniqueKey) : this(getUniqueKey)
        {
            __instance = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        public Map(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer, Func<TKey, IEnumerable<TKey>, TKey> getUniqueKey) : this(getUniqueKey)
        {
            __instance = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        public Map(IDictionary<TKey, TValue> dictionary, Func<TKey, IEnumerable<TKey>, TKey> getUniqueKey) : this(getUniqueKey)
        {
            __instance = new Dictionary<TKey, TValue>(dictionary);
        }

        public Map(int capacity, Func<TKey, IEnumerable<TKey>, TKey> getUniqueKey) : this(getUniqueKey)
        {
            __instance = new Dictionary<TKey, TValue>(capacity);
        }

        public void Add(TKey key, TValue value)
        {
            if (__instance == null)
                __instance = new Dictionary<TKey, TValue>();

            __instance.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return __instance != null && __instance.ContainsKey(key);
        }

        public bool ContainsValue(TValue value)
        {
            return __instance != null && __instance.ContainsValue(value);
        }

        public bool Remove(TKey key)
        {
            return __instance != null && __instance.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (__instance == null)
            {
                value = default(TValue);

                return false;
            }

            return __instance.TryGetValue(key, out value);
        }

        public void Clear()
        {
            if (__instance != null)
                __instance.Clear();
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            if (__instance == null)
                __instance = new Dictionary<TKey, TValue>();

            return __instance.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            if (__instance == null)
                __instance = new Dictionary<TKey, TValue>();

            ((ICollection<KeyValuePair<TKey, TValue>>)__instance).Add(item);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return __instance != null && ((ICollection<KeyValuePair<TKey, TValue>>)__instance).Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (__instance == null)
                return;

            ((ICollection<KeyValuePair<TKey, TValue>>)__instance).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return __instance != null && ((ICollection<KeyValuePair<TKey, TValue>>)__instance).Remove(item);
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return __instance == null ? null : __instance.Keys;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return __instance == null ? null : __instance.Values;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            int count;
            if (__instance == null)
                count = 0;
            else
                count = __instance.Count;

            if (count > 0)
            {
                _keys = new TKey[count];
                _values = new TValue[count];

                int index = 0;
                foreach (KeyValuePair<TKey, TValue> keyValuePair in __instance)
                {
                    _keys[index] = keyValuePair.Key;
                    _values[index] = keyValuePair.Value;

                    ++index;
                }
            }
            else
            {
                _keys = null;
                _values = null;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            __instance = null;

            int count = Mathf.Min(_keys == null ? 0 : _keys.Length, _values == null ? 0 : _values.Length);
            if (count > 0)
            {
                __instance = new Dictionary<TKey, TValue>(count);//, _equalityComparer == null ? null : _equalityComparer.instance as IEqualityComparer<TKey>);
                TKey key;
                for (int i = 0; i < count; ++i)
                {
                    key = _keys[i];
                    if(__instance.ContainsKey(key))
                    {
                        if (__getUniqueKey == null)
                            continue;

                        key = __getUniqueKey(key, _keys);

                        _keys[i] = key;
                    }

                    __instance[key] = _values[i];
                }
            }
        }
    }

    public class Map<T> : Map<string, T>
    {
        public Map(IEqualityComparer<string> comparer = null) : base(comparer, NameHelper.MakeUnique)
        {

        }

        public Map(IDictionary<string, T> dictionary) : base(dictionary, NameHelper.MakeUnique)
        {

        }

        public Map(int capacity) : base(capacity, NameHelper.MakeUnique)
        {

        }
    }

    public static class MapUtility
    {
        public static int GetUniqueValue(int key, IEnumerable<int> keys)
        {
            if (keys != null)
            {
                foreach (int temp in keys)
                {
                    if (temp == key)
                        return GetUniqueValue(key + 1, keys);
                }
            }

            return key;
        }

        public static short GetUniqueValue(short key, IEnumerable<short> keys)
        {
            if (keys != null)
            {
                foreach (int temp in keys)
                {
                    if (temp == key)
                        return GetUniqueValue((short)(key + 1), keys);
                }
            }

            return key;
        }

        public static byte GetUniqueValue(byte key, IEnumerable<byte> keys)
        {
            if (keys != null)
            {
                foreach (byte temp in keys)
                {
                    if (temp == key)
                        return GetUniqueValue((byte)(key + 1), keys);
                }
            }

            return key;
        }

        public static sbyte GetUniqueValue(sbyte key, IEnumerable<sbyte> keys)
        {
            if (keys != null)
            {
                foreach (sbyte temp in keys)
                {
                    if (temp == key)
                        return GetUniqueValue((sbyte)(key + 1), keys);
                }
            }

            return key;
        }
    }
}