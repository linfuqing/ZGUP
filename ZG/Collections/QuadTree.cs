using System;
using System.Collections;
using System.Collections.Generic;

namespace ZG
{
    public class QuadTree<T> : IEnumerable<QuadTree<T>.Item>
    {
        public class Item
        {
            public T userData;

            private QuadTree<T> __quadTree;
            private Node __node;
            private LinkedListNode<Item> __item;
            private Rectangle __rectangle;
            private int __flag;

            public QuadTree<T> quadTree
            {
                get
                {
                    return __quadTree;
                }
            }

            public Rectangle rectangle
            {
                get
                {
                    return __rectangle;
                }

                set
                {
                    __rectangle = value;

                    Attach(__quadTree);
                }
            }

            public int flag
            {
                get
                {
                    return __flag;
                }

                set
                {
                    __flag = value;

                    if (__node != null)
                        __node.ResetLocalFlag();
                }
            }

            public bool Reset()
            {
                if (__node == null)
                    return false;

                if (__item == null)
                    __item = new LinkedListNode<Item>(this);

                return __node.Add(__item);
            }
            
            public bool Attach(QuadTree<T> quadTree)
            {
                if (quadTree == null)
                    return false;

                int max = __MAXINUM_BIT_TABLE.Length - 1;
                Rectangle rectangle = __rectangle;
                rectangle.min.x = Math.Max(0, __rectangle.min.x);
                rectangle.min.y = Math.Max(0, __rectangle.min.y);
                rectangle.max.x = Math.Min(max, __rectangle.max.x);
                rectangle.max.y = Math.Min(max, __rectangle.max.y);

                Node node = quadTree.__FindTreeNode(rectangle.min.x, rectangle.min.y, rectangle.max.x, rectangle.max.y);
                if (node == null || node == __node)
                    return false;

                if (__item == null)
                    __item = new LinkedListNode<Item>(this);
                else if (__node != null)
                {
                    if (__node.Remove(__item))
                        __node.ResetLocalFlag();
                    else
                        return false;
                }

                if (!node.Add(__item))
                    return false;

                node.ResetLocalFlag();

                __node = node;

                __quadTree = quadTree;

                return true;
            }

            public bool Detach()
            {
                if (__node == null)
                    return false;

                if (__node.Remove(__item))
                {
                    __node.ResetLocalFlag();
                    __node = null;

                    __quadTree = null;

                    return true;
                }

                return false;
            }
        }

        private class Node
        {
            private Node __parent;
            private Node __leftUp;
            private Node __leftDown;
            private Node __rightUp;
            private Node __rightDown;
            private LinkedList<Item> __items;
            private int __localFlag;
            private int __worldFlag;

            public int flag
            {
                get
                {
                    return __worldFlag;
                }
            }

            public void Setup(Node parent, Node leftUp, Node leftDown, Node rightUp, Node rightDown)
            {
                __parent = parent;
                __leftUp = leftUp;
                __leftDown = leftDown;
                __rightUp = rightUp;
                __rightDown = rightDown;
            }

            public bool Add(LinkedListNode<Item> item)
            {
                if (item == null || item.Value == null)
                    return false;

                if (__items == null)
                    __items = new LinkedList<Item>();
                else if (item.List == __items)
                    return false;

                __items.AddLast(item);

                return true;
            }

            public bool Remove(LinkedListNode<Item> item)
            {
                if (item == null || item.List != __items)
                    return false;

                __items.Remove(item);
                
                return true;
            }

            public void Reset()
            {
                Item item;
                LinkedListNode<Item> node = __items == null ? null : __items.First;
                while (node != null)
                {
                    item = node.Value;
                    node = node.Next;

                    if (item != null)
                        item.Attach(item.quadTree);
                }
            }

            public void ResetLocalFlag()
            {
                __localFlag = 0;
                if (__items != null)
                {
                    foreach (Item item in __items)
                        __localFlag |= item == null ? 0 : item.flag;
                }

                ResetWorldFlag();
            }

            public void ResetWorldFlag()
            {
                __worldFlag = __localFlag;

                if (__leftUp != null)
                    __worldFlag |= __leftUp.__worldFlag;

                if (__leftDown != null)
                    __worldFlag |= __leftDown.__worldFlag;

                if (__rightUp != null)
                    __worldFlag |= __rightUp.__worldFlag;

                if (__rightDown != null)
                    __worldFlag |= __rightDown.__worldFlag;

                if (__parent != null)
                    __parent.ResetWorldFlag();
            }

            public Item Find(Predicate<Item> predicate, int minX, int minY, int maxX, int maxY, int flag)
            {
                if ((__localFlag & flag) == 0)
                    return null;
                
                Rectangle rectangle;
                QuadTree<T> quadTree;
                Item item;
                for(LinkedListNode<Item> node = __items == null ? null : __items.First; node != null; node = node.Next)
                {
                    item = node.Value;
                    quadTree = item == null ? null : item.quadTree;
                    if (quadTree != null && (item.flag & flag) != 0)
                    {
                        rectangle = item.rectangle;
                        if (rectangle.IsContain(minX, minY, maxX, maxY) ||
                            rectangle.IsIntersect(minX, minY, maxX, maxY))
                        {
                            if (predicate == null || predicate(item))
                                return item;
                        }
                    }
                }

                return null;
            }

            public Item Find(Predicate<Item> predicate, int flag)
            {
                if ((__localFlag & flag) == 0)
                    return null;
                
                QuadTree<T> quadTree;
                Item item;
                for (LinkedListNode<Item> node = __items == null ? null : __items.First; node != null; node = node.Next)
                {
                    item = node.Value;
                    quadTree = item == null ? null : item.quadTree;
                    if (quadTree != null && (item.flag & flag) != 0)
                    {
                        if (predicate == null || predicate(item))
                            return item;
                    }
                }

                return null;
            }

            public int Search(int minX, int minY, int maxX, int maxY, int flag)
            {
                if ((__localFlag & flag) == 0)
                    return 0;

                int count = 0;
                Rectangle rectangle;
                QuadTree<T> quadTree;
                Item item;
                LinkedListNode<Item> current = __items == null ? null : __items.First, next;
                while(current != null)
                {
                    item = current.Value;
                    quadTree = item == null ? null : item.quadTree;
                    if (quadTree != null && (item.flag & flag) != 0)
                    {
                        rectangle = item.rectangle;
                        if (rectangle.IsContain(minX, minY, maxX, maxY) ||
                            rectangle.IsIntersect(minX, minY, maxX, maxY))
                        {
                            next = current.Next;
                            __items.Remove(current);

                            if (quadTree.__searchList == null)
                                quadTree.__searchList = new LinkedList<Item>();

                            quadTree.__searchList.AddLast(current);

                            ++count;

                            current = next;

                            continue;
                        }
                    }

                    current = current.Next;
                }

                return count;
            }

            public int Search(int flag)
            {
                if ((__localFlag & flag) == 0 || __items == null)
                    return 0;

                int count = 0;
                QuadTree<T> quadTree;
                Item item;
                LinkedListNode<Item> current = __items.First, next;
                while (current != null)
                {
                    item = current.Value;
                    quadTree = item == null ? null : item.quadTree;
                    if (quadTree != null && (item.flag & flag) != 0)
                    {
                        next = current.Next;
                        __items.Remove(current);

                        if (quadTree.__searchList == null)
                            quadTree.__searchList = new LinkedList<Item>();

                        quadTree.__searchList.AddLast(current);

                        ++count;

                        current = next;

                        continue;
                    }

                    current = current.Next;
                }

                return count;
            }
        }

        private static readonly int[] __MAXINUM_BIT_TABLE = new int[256]
        {
            0, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
        };

        public static readonly int MAXINUM_DEPTH = (int)Math.Round(Math.Log(__MAXINUM_BIT_TABLE.Length, 2.0f)) + 1;

        private LinkedList<Item> __searchList;

        private Node[][] __levelNodes;
        
        private int __depth;
        
        public int depth
        {
            get
            {
                return __depth;
            }

            set
            {
#if DEBUG
                if (value <= 0 || value > MAXINUM_DEPTH)
                    throw new ArgumentOutOfRangeException();
#endif

                int currentDepth = __levelNodes == null ? 0 : __levelNodes.Length;
                __depth = Math.Min(__depth, currentDepth);
                if (value == __depth)
                    return;

                int previousDepth = __depth;
                __depth = value;

                if (value > currentDepth)
                {
                    //Array.Resize(ref __levelNodes, value);
                    int i;
                    Node[][] levelNodes = new Node[value][];
                    for (i = 0; i < currentDepth; ++i)
                        levelNodes[i] = __levelNodes[i];

                    __levelNodes = levelNodes;

                    Node[] nodes;

                    int nodeCount, j;
                    for (i = currentDepth; i < value; i++)
                    {
                        nodeCount = 1 << i;
                        nodeCount *= nodeCount;

                        nodes = new Node[nodeCount];

                        for (j = 0; j < nodeCount; ++j)
                            nodes[j] = new Node();

                        __levelNodes[i] = nodes;
                    }

                    int x, y, levelDimension, levelIndex;
                    for (i = currentDepth; i < value; ++i)
                    {
                        levelDimension = 1 << i;
                        levelIndex = 0;

                        for (y = 0; y < levelDimension; ++y)
                        {
                            for (x = 0; x < levelDimension; ++x)
                            {
                                __levelNodes[i][levelIndex].Setup(
                                    __GetNodeFromLevelXY(i - 1, (x >> 1), (y >> 1)),
                                    __GetNodeFromLevelXY(i + 1, (x << 1), (y << 1)),
                                    __GetNodeFromLevelXY(i + 1, (x << 1) + 1, (y << 1)),
                                    __GetNodeFromLevelXY(i + 1, (x << 1), (y << 1) + 1),
                                    __GetNodeFromLevelXY(i + 1, (x << 1) + 1, (y << 1) + 1));

                                ++levelIndex;
                            }
                        }
                    }
                }
                
                if (__levelNodes != null)
                {
                    Node[] nodes;
                    if (previousDepth < value)
                    {
                        nodes = __levelNodes[previousDepth];
                        if (nodes != null)
                        {
                            foreach (Node node in nodes)
                                node.Reset();
                        }
                    }
                    else
                    {
                        for (int i = value; i < previousDepth; ++i)
                        {
                            nodes = __levelNodes[i];
                            if (nodes != null)
                            {
                                foreach (Node node in nodes)
                                    node.Reset();
                            }
                        }
                    }
                }
            }
        }

        public void Reset()
        {
            Item item;
            LinkedListNode<Item> current = __searchList == null ? null : __searchList.First, next;
            while (current != null)
            {
                next = current.Next;
                __searchList.Remove(current);
                item = current.Value;
                if (item != null)
                    item.Reset();

                current = next;
            }
        }

        public Item Find(Predicate<Item> predicate, int minX, int minY, int maxX, int maxY, int flag, ref int level)
        {
            if (level >= __depth)
                return null;

            int max = __MAXINUM_BIT_TABLE.Length - 1;
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(max, maxX);
            maxY = Math.Min(max, maxY);

            bool isNextLevel;
            int shift, currentMinX, currentMaxX, currentMinY, currentMaxY, i, j;
            Node node;
            Item item;

            do
            {
                isNextLevel = false;

                shift = MAXINUM_DEPTH - level - 1;
                currentMinX = minX >> shift;
                currentMaxX = maxX >> shift;
                currentMinY = minY >> shift;
                currentMaxY = maxY >> shift;
                for (j = currentMinY; j <= currentMaxY; ++j)
                {
                    for (i = currentMinX; i <= currentMaxX; ++i)
                    {
                        node = __GetNodeFromLevelXY(level, i, j);

                        if (node != null && (node.flag & flag) != 0)
                        {
                            isNextLevel = true;

                            item = j == currentMinY || j == currentMaxY || i == currentMinX || i == currentMaxX ? 
                                node.Find(predicate, minX, minY, maxX, maxY, flag) : 
                                node.Find(predicate, flag);
                            if (item != null)
                                return item;
                        }
                    }
                }

                ++level;
            }
            while (isNextLevel && level < __depth);

            return null;
        }

        public Item Find(Predicate<Item> predicate, int minX, int minY, int maxX, int maxY, int flag)
        {
            int level = 0;
            return Find(predicate, minX, minY, maxX, maxY, flag, ref level);
        }

        public Item Find(Predicate<Item> predicate, Rectangle rectangle, int flag)
        {
            return Find(predicate, rectangle.min.x, rectangle.min.y, rectangle.max.x, rectangle.max.y, flag);
        }

        public bool Test(int minX, int minY, int maxX, int maxY, int flag, ref int level)
        {
            return Find(null, minX, minY, maxX, maxY, flag, ref level) != null;
        }

        public bool Test(int minX, int minY, int maxX, int maxY, int flag)
        {
            int level = 0;
            return Test(minX, minY, maxX, maxY, flag, ref level);
        }
        
        public bool Test(Rectangle rectangle, int flag)
        {
            return Test(rectangle.min.x, rectangle.min.y, rectangle.max.x, rectangle.max.y, flag);
        }

        public int Search(int minX, int minY, int maxX, int maxY, int flag, ref int level)
        {
            if (level >= __depth)
                return 0;

            int max = __MAXINUM_BIT_TABLE.Length - 1;
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(max, maxX);
            maxY = Math.Min(max, maxY);

            bool isNextLevel;
            int shift, currentMinX, currentMaxX, currentMinY, currentMaxY, i, j, count = 0;
            Node node;
            
            do
            {
                isNextLevel = false;

                shift = MAXINUM_DEPTH - level - 1;
                currentMinX = minX >> shift;
                currentMaxX = maxX >> shift;
                currentMinY = minY >> shift;
                currentMaxY = maxY >> shift;
                for (j = currentMinY; j <= currentMaxY; ++j)
                {
                    for (i = currentMinX; i <= currentMaxX; ++i)
                    {
                        node = __GetNodeFromLevelXY(level, i, j);

                        if (node != null && (node.flag & flag) != 0)
                        {
                            isNextLevel = true;

                            if (j == currentMinY || j == currentMaxY || i == currentMinX || i == currentMaxX)
                                count += node.Search(minX, minY, maxX, maxY, flag);
                            else
                                count += node.Search(flag);
                        }
                    }
                }

                ++level;
            }
            while (isNextLevel && level < __depth);

            return count;
        }

        public int Search(int minX, int minY, int maxX, int maxY, int flag)
        {
            int level = 0;
            return Search(minX, minY, maxX, maxY, flag, ref level);
        }

        public int Search(Rectangle rectangle, int flag)
        {
            return Search(rectangle.min.x, rectangle.min.y, rectangle.max.x, rectangle.max.y, flag);
        }
        
        public IEnumerator<Item> GetEnumerator()
        {
            if (__searchList == null)
                __searchList = new LinkedList<Item>();

            return __searchList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private Node __FindTreeNode(int minX, int minY, int maxX, int maxY)
        {
            int level, x, y;

            __FindTreeNodeInfo(minX, minY, maxX, maxY, out level, out x, out y);

            return __GetNodeFromLevelXY(level, x, y);
        }

        private void __FindTreeNodeInfo(int minX, int minY, int maxX, int maxY, out int level, out int x, out int y)
        {
            int patternX = minX ^ maxX,
                patternY = minY ^ maxY,
                bitPattern = Math.Max(patternX, patternY),
                highBit = bitPattern < __MAXINUM_BIT_TABLE.Length ? __MAXINUM_BIT_TABLE[bitPattern] : (MAXINUM_DEPTH - 1);

            level = Math.Min(MAXINUM_DEPTH - highBit - 1, depth - 1);

            int shift = MAXINUM_DEPTH - level - 1;

            x = maxX >> shift;
            y = maxY >> shift;
        }

        private Node __GetNodeFromLevelXY(int level, int x, int y)
        {
            if (level >= 0 && level < __depth)
            {
                Node[] nodes = __levelNodes[level];
                if (nodes != null)
                {
                    int index = (y << level) + x;
                    if (index >= 0 && index < nodes.Length)
                        return nodes[index];
                }
            }

            return null;
        }
    }
}
