using System;
using System.Collections.Generic;

namespace ZG
{
    public class MemoryWrapper
    {
        private Dictionary<int, int> __bufferSegmentsToBeAlloc;
        private SortedList<int, int> __bufferSegmentsToBeFree;

        public int length
        {
            get;

            private set;
        }

        public int capacity
        {
            get;

            private set;
        }

        public MemoryWrapper()
        {
            __bufferSegmentsToBeAlloc = new Dictionary<int, int>();
            __bufferSegmentsToBeFree = new SortedList<int, int>();
        }

        public int Alloc(int count)
        {
            int offset = -1;

            foreach (var pair in __bufferSegmentsToBeFree)
            {
                if (pair.Value >= count)
                {
                    offset = pair.Key;

                    break;
                }
            }

            if (offset == -1)
            {
                offset = this.length;

                __bufferSegmentsToBeAlloc.Add(offset, count);

                this.length = offset + count;

                capacity = Math.Max(capacity, this.length);

                return offset;
            }

            int length = __bufferSegmentsToBeFree[offset];

            __bufferSegmentsToBeFree.Remove(offset);

            if (length > count)
                __Free(offset + count, length - count);

            __bufferSegmentsToBeAlloc.Add(offset, count);

            return offset;
        }

        public bool Free(int offset)
        {
            if (!__bufferSegmentsToBeAlloc.TryGetValue(offset, out int count) ||
                !__bufferSegmentsToBeAlloc.Remove(offset))
                return false;

            __Free(offset, count);

            return true;
        }

        private void __Free(int startIndex, int count)
        {
            int length = startIndex + count;
            if (__bufferSegmentsToBeFree.TryGetValue(length, out int freeCount))
            {
                count += freeCount;

                __bufferSegmentsToBeFree.Remove(length);
            }

            var keys = __bufferSegmentsToBeFree.Keys;
            int index = keys.BinarySearch(startIndex);
            if (index >= 0)
            {
                index = keys[index];
                freeCount = __bufferSegmentsToBeFree[index];
                if (index + freeCount == startIndex)
                {
                    count += freeCount;

                    startIndex = index;

                    __bufferSegmentsToBeFree.Remove(index);
                }
            }

            //length = startIndex + count;
            if (length < this.length)
                __bufferSegmentsToBeFree[startIndex] = count;
            else
                this.length = startIndex;//length;
        }
    }
}