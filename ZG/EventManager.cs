using System;
using System.Collections.Generic;

namespace ZG
{
    public class EventManager
    {
        [Serializable]
        public struct Info
        {
            public float probability;
            public int nodeIndex;
        }

        [Serializable]
        public struct Node
        {
            public float time;
            public Info[] triggers;
            public Info[] killers;
        }

        private struct Item
        {
            public double time;
            public int nodeIndex;

            public Item(double time, int nodeIndex)
            {
                this.time = time;
                this.nodeIndex = nodeIndex;
            }
        }

        public Action<int, double> onTriggered;
        public Action<int, double> onKilled;

        private LinkedList<Item> __items;
        private Node[] __nodes;
        private Random __random;
        private double __time;

        public double time
        {
            get
            {
                return __time;
            }
        }

        public EventManager(Node[] nodes)
        {
            __nodes = nodes;
        }

        public EventManager(Node[] nodes, int seed) : this(nodes)
        {
            __random = new Random(seed);
        }

        public void Run(float elapsedTime)
        {
            if (__items == null)
                return;

            __time += elapsedTime;

            bool isTriggered;
            int numNodes = __nodes == null ? 0 : __nodes.Length;
            float random, probability;
            Node node;
            Item item;
            LinkedListNode<Item> temp = __items.First;
            while (temp != null)
            {
                item = temp.Value;

                if (__time < item.time)
                    break;

                if (onKilled != null)
                    onKilled(item.nodeIndex, item.time);

                __items.RemoveFirst();

                if (item.nodeIndex >= 0 && item.nodeIndex < numNodes)
                {
                    node = __nodes[item.nodeIndex];
                    if (node.killers != null && node.killers.Length > 0)
                    {
                        isTriggered = false;

                        if (__random == null)
                            __random = new Random();

                        random = (float)__random.NextDouble();
                        probability = 0.0f;

                        foreach (Info killer in node.killers)
                        {
                            probability += killer.probability;
                            if (probability > 1.0f)
                            {
                                probability -= 1.0f;

                                random = (float)__random.NextDouble();

                                isTriggered = false;
                            }

                            if (!isTriggered && random < probability)
                            {
                                isTriggered = true;

                                Kill(killer.nodeIndex, item.time);
                            }
                        }
                    }

                    if (node.triggers != null && node.triggers.Length > 0)
                    {
                        isTriggered = false;

                        if (__random == null)
                            __random = new Random();

                        random = (float)__random.NextDouble();
                        probability = 0.0f;

                        foreach (Info trigger in node.triggers)
                        {
                            probability += trigger.probability;
                            if (probability > 1.0f)
                            {
                                probability -= 1.0f;

                                random = (float)__random.NextDouble();

                                isTriggered = false;
                            }

                            if (!isTriggered && random < probability)
                            {
                                isTriggered = true;

                                Trigger(trigger.nodeIndex, item.time);
                            }
                        }
                    }
                }

                temp = __items.First;
            }
        }

        public bool Trigger(int index, double time)
        {
            if (index < 0)
                return false;

            int numNodes = __nodes == null ? 0 : __nodes.Length;
            if (numNodes <= index)
                return false;

            time = Math.Max(time, __time);

            Item item = new Item(time + __nodes[index].time, index);
            if (__items == null)
                __items = new LinkedList<Item>();

            LinkedListNode<Item> node = __items.First;
            while (node != null)
            {
                if (item.time < node.Value.time)
                    break;

                node = node.Next;
            }

            if (node == null)
                __items.AddLast(item);
            else
                __items.AddBefore(node, item);

            if (onTriggered != null)
                onTriggered(index, time);

            return true;
        }

        public bool Kill(int index, double time)
        {
            for (LinkedListNode<Item> node = __items == null ? null : __items.First; node != null; node = node.Next)
            {
                if (node.Value.nodeIndex == index)
                {
                    if (onKilled != null)
                        onKilled(index, Math.Max(time, __time));

                    __items.Remove(node);

                    return true;
                }
            }

            return false;
        }
    }
}