using System;
using System.Collections;
using System.Collections.Generic;

namespace ZG
{
    public struct AssetHandle : IEquatable<AssetHandle>
    {
        public int index;
        public int version;

        public bool Equals(AssetHandle other)
        {
            return index == other.index && version == other.version;
        }

        public override int GetHashCode()
        {
            return index;
        }

        public override string ToString()
        {
            return $"AssetHandle(Index:{index}, Version:{version})";
        }
    }

    [Serializable]
    public class Assets<T> : IEnumerable<T>
    {
        [Serializable]
        private struct Data
        {
            public int version;
            public T value;
        }

        [Serializable]
        private struct Info
        {
            public int count;
            public int index;
        }

        public class Enumerator : Object, IEnumerator<T>
        {
            private Dictionary<T, Info>.KeyCollection.Enumerator __instance;

            public T Current => __instance.Current;

            internal static Enumerator Create(Assets<T> assets)
            {
                var result = Create<Enumerator>();

                if (assets.__infos == null)
                    assets.__infos = new Dictionary<T, Info>();

                result.__instance = assets.__infos.Keys.GetEnumerator();

                return result;
            }

            public bool MoveNext()
            {
                return __instance.MoveNext();
            }

            void IEnumerator.Reset()
            {
            }

            object IEnumerator.Current => __instance.Current;
        }

        private Pool<Data> __data;
        private Dictionary<T, Info> __infos;

        public T this[in AssetHandle handle]
        {
            get
            {
                Data data = __data[handle.index];
                if (data.version != handle.version)
                    throw new IndexOutOfRangeException();

                return data.value;
            }
        }

        public Assets()
        {
        }

        public Assets(IEqualityComparer<T> comparer)
        {
            __infos = new Dictionary<T, Info>(comparer);
        }

        public bool Contains(in T value) => __infos != null && __infos.ContainsKey(value);

        public bool TryGetHandle(in T value, out AssetHandle handle)
        {
            if(__infos != null && __infos.TryGetValue(value, out var info))
            {
                handle.version = __data[info.index].version;
                handle.index = info.index;

                return true;
            }

            handle = default;

            return false;
        }

        public AssetHandle Add(in T value, out int count)
        {
            if (__infos == null)
                __infos = new Dictionary<T, Info>();

            AssetHandle assetHandle;
            if (!__infos.TryGetValue(value, out Info info))
            {
                if (__data == null)
                    __data = new Pool<Data>();

                info.count = 1;
                info.index = __data.nextIndex;

                __data.TryGetValue(info.index, out Data data);
                assetHandle.version = ++data.version;
                data.value = value;

                __data.Insert(info.index, data);
            }
            else
            {
                ++info.count;

                assetHandle.version = __data[info.index].version;
            }

            count = info.count;

            assetHandle.index = info.index;

            __infos[value] = info;

            return assetHandle;
        }

        public AssetHandle Add(in T value) => Add(value, out _);

        public int Remove(in T value)
        {
            Info info = __infos[value];

            if (--info.count < 1)
            {
                __data.RemoveAt(info.index);

                __infos.Remove(value);
            }
            else
                __infos[value] = info;

            return info.count;
        }

        public int Remove(in AssetHandle handle)
        {
            return Remove(this[handle]);
        }

        public int Remove(in AssetHandle handle, out T value)
        {
            value = this[handle];

            return Remove(value);
        }

        public void Clear()
        {
            if(__data != null)
                __data.Clear();

            if(__infos != null)
                __infos.Clear();
        }

        public Enumerator GetEnumerator()
        {
            return Enumerator.Create(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}