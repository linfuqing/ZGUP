using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ZG
{
    /// <summary>
    /// <see cref="System.Array"/>
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public struct ByteArray<T> : IList<T> where T : struct
    {
        public struct Enumerator : IEnumerator<T>
        {
            private IntPtr __instance;
            private int __count;
            private int __index;

            public Enumerator(IntPtr instance, int count)
            {
                __instance = instance;
                __count = count;
                __index = -1;
            }

            public T Current
            {
                get
                {
                    return (T)Marshal.PtrToStructure(new IntPtr(__instance.ToInt64() + __index * Marshal.SizeOf(typeof(T))), typeof(T));
                }
            }

            public bool MoveNext()
            {
                ++__index;
                return __index < __count;
            }
            
            public void Reset()
            {
                __index = -1;
            }
            
            void IDisposable.Dispose()
            {

            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }

        private IntPtr __instance;
        private int __count;

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                return __count;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= __count)
                    throw new IndexOutOfRangeException();

                return (T)Marshal.PtrToStructure(new IntPtr(__instance.ToInt64() + index * Marshal.SizeOf(typeof(T))), typeof(T));
            }

            set
            {
                if (index < 0 || index >= __count)
                    throw new IndexOutOfRangeException();

                Marshal.StructureToPtr(value, new IntPtr(__instance.ToInt64() + index * Marshal.SizeOf(typeof(T))), false);
            }
        }

        public ByteArray(IntPtr instance, int count)
        {
            __instance = instance;
            __count = count;
        }

        public bool Contains(T item)
        {
            for (int i = 0; i < __count; ++i)
            {
                if (item.Equals(this[i]))
                    return true;
            }

            return false;
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < __count; ++i)
            {
                if (item.Equals(this[i]))
                    return i;
            }

            return -1;
        }

        public void CopyTo(T[] array, int count)
        {
            count = Math.Min(count, __count);
            if (count < 1)
                return;

            long offset = __instance.ToInt64(), size = Marshal.SizeOf(typeof(T));
            for (int i = 0; i < count; ++i)
            {
                array[i] = (T)Marshal.PtrToStructure(new IntPtr(offset), typeof(T));

                offset += size;
            }
        }

        public T[] ToArray()
        {
            if (__count < 1)
                return null;

            T[] result = new T[__count];
            long offset = __instance.ToInt64(), size = Marshal.SizeOf(typeof(T));
            for (int i = 0; i < __count; ++i)
            {
                result[i] = (T)Marshal.PtrToStructure(new IntPtr(offset), typeof(T));

                offset += size;
            }

            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(__instance, __count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }
    }
}