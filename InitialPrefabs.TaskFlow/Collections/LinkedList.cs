using System;

namespace InitialPrefabs.TaskFlow.Collections {

    public class LinkedList<T0> where T0 : unmanaged, IEquatable<T0> {

        public struct Node<T1> : IEquatable<Node<T1>> where T1 : unmanaged, IEquatable<T1> {
            public short Previous;
            public short Next;
            public T1 Value;

            public readonly bool Equals(Node<T1> other) {
                return other.Previous == Previous && other.Next == Next && other.Value.Equals(Value);
            }
        }

        internal Node<T0>[] Nodes;
        internal DynamicArray<short> FreeList;
        internal DynamicArray<short> UseList;

        public int Count => UseList.Count;

        internal short Head;
        internal short Tail;

        public LinkedList(int capacity) {
            Nodes = new Node<T0>[capacity];
            FreeList = new DynamicArray<short>(capacity);
            UseList = new DynamicArray<short>(capacity);

            for (short i = 0; i < capacity; i++) {
                FreeList.Add(i);
            }

            Head = -1;
            Tail = -1;
        }

        public LinkedList(int capacity, T0 value) : this(capacity) {
            AddFirst(value);
        }

        public void AddFirst(T0 item) {
            // Take the first node
            var freeIndex = FreeList[0];
            FreeList.RemoveAtSwapback(0);
            UseList.Add(freeIndex);

            Nodes[freeIndex] = new Node<T0> {
                Previous = -1,
                Value = item,
                Next = FreeList[0]
            };

            Head = freeIndex;
            Tail = freeIndex;
        }

        public void Append(T0 item) {
            if (UseList.Count == 0) {
                AddFirst(item);
            } else {
                var last = UseList[^1];
                var lastNode = Nodes[last];
                var nextFree = lastNode.Next;

                var idx = FreeList.Find(element => element == nextFree);
#if DEBUG
                if (idx == -1) {
                    throw new InvalidOperationException();
                }
#endif

                FreeList.RemoveAtSwapback(idx);
                UseList.Add(nextFree);
                var next = FreeList[0];
                var node = new Node<T0> {
                    Next = next,
                    Value = item,
                    Previous = last
                };
                Nodes[nextFree] = node;
                Tail = nextFree;
            }
        }
    }
}

