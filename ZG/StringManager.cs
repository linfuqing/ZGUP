using System;
using System.Collections.Generic;

namespace ZG
{
    [Serializable]
    public struct StringHandle : IEquatable<StringHandle>
    {
        public int value;

        public static readonly StringHandle empty = default;

        public override int GetHashCode()
        {
            return value;
        }

        public override bool Equals(object obj)
        {
            return Equals((StringHandle)obj);
        }

        public bool Equals(StringHandle handle)
        {
            return value == handle.value;
        }

        public static bool operator==(in StringHandle src, in StringHandle dst)
        {
            return src.value == dst.value;
        }

        public static bool operator !=(in StringHandle src, in StringHandle dst)
        {
            return src.value != dst.value;
        }

        public static implicit operator string(in StringHandle value)
        {
            return value.Get();
        }

        public static implicit operator StringHandle(string value)
        {
            return value.Intern();
        }
    }

    public static class StringManager
    {
        private static List<string> __values;
        private static Dictionary<string, int> __map;

        public static StringHandle Intern(this string value)
        {
            if (__map == null)
                __map = new Dictionary<string, int>();

            StringHandle handle;
            if (__map.TryGetValue(value, out handle.value))
            {
                ++handle.value;

                return handle;
            }

            if (__values == null)
                __values = new List<string>();

            handle.value = __values.Count;
            __values.Add(value);

            __map[value] = handle.value++;

            return handle;
        }

        public static string Get(in this StringHandle handle)
        {
            return __values[handle.value - 1];
        }

        public static bool IsVail(in this StringHandle handle)
        {
            return handle.value >= 0 && handle.value < (__values == null ? 0 : __values.Count);
        }
    }
}