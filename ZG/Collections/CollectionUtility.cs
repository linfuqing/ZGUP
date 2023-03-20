using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZG
{
    public interface IReadOnlyListWrapper<out TValue, in TList>
    {
        int GetCount(TList list);

        TValue Get(TList list, int index);
    }

    public interface IWriteOnlyListWrapper<in TValue, TList>
    {
        int GetCount(in TList list);

        void SetCount(ref TList list, int value);

        void Set(ref TList list, TValue value, int index);
    }

    public struct WriteOnlyListWrapper<TValue, TList> : IWriteOnlyListWrapper<TValue, TList> where TList : IList<TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount(in TList list) => list.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCount(ref TList list, int value)
        {
            if (value == 0)
                list.Clear();
            else
            {
                int count = list.Count;
                for (int i = count; i < value; ++i)
                    list.Add(default);

                for (int i = count - 1; i >= value; --i)
                    list.RemoveAt(i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(ref TList list, TValue value, int index)
        {
            for (int i = list.Count; i <= index; ++i)
                list.Add(default);

            list[index] = value;
        }
    }

    public struct ReadOnlyListWrapper<TValue, TList> : IReadOnlyListWrapper<TValue, TList> where TList : IReadOnlyList<TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount(TList list) => list.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get(TList list, int index) => list[index];
    }

    public struct Comparer<T> : IComparer<T> where T : IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return x.CompareTo(y);
        }
    }

    public static class CollectionUtility
    {
        public struct ListWrapper<TValue, TList> : IReadOnlyListWrapper<TValue, TList> where TList : IList<TValue>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetCount(TList list) => list.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TValue Get(TList list, int index) => list[index];
        }

        /// <summary>
        /// Find the max index of element which less equal than <see cref="value"/>.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TList"></typeparam>
        /// <typeparam name="TComparer"></typeparam>
        /// <typeparam name="TListWrapper"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <param name="comparer"></param>
        /// <param name="listWrapper"></param>
        /// <returns></returns>
        public static int BinarySearch<TValue, TList, TComparer, TListWrapper>(this TList list, in TValue value, in TComparer comparer, in TListWrapper listWrapper)
            where TComparer : IComparer<TValue> 
            where TListWrapper : IReadOnlyListWrapper<TValue, TList>
        {
            int index = -1, count = listWrapper.GetCount(list), middle;
            while (count > 0)
            {
                middle = (count + 1) >> 1;
                if (comparer.Compare(listWrapper.Get(list, index + middle), value) > 0)
                {
                    if (middle < 2)
                        break;

                    count = middle;
                }
                else
                {
                    index += middle;

                    count -= middle;
                }
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearchReadOnly<TValue, TList, TComparer>(this TList list, in TValue value, in TComparer comparer)
            where TList : IReadOnlyList<TValue>
            where TComparer : IComparer<TValue>
        {
            return BinarySearch(list, value, comparer, new ReadOnlyListWrapper<TValue, TList>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TValue, TList, TComparer>(this TList list, in TValue value, in TComparer comparer) 
            where TList : IList<TValue>
            where TComparer : IComparer<TValue>
        {
            return BinarySearch(list, value, comparer, new ListWrapper<TValue, TList>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TValue, TList>(this TList list, in TValue value)
            where TList : IList<TValue>
        {
            return BinarySearch(list, value, System.Collections.Generic.Comparer<TValue>.Default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TKey, TValue>(this SortedList<TKey, TValue> list, in TKey key)
        {
            return BinarySearch(list.Keys, key, list.Comparer);
        }
    }
}