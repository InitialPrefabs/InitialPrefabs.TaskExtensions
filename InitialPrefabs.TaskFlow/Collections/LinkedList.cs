using System;
using System.Collections;
using System.Collections.Generic;

namespace InitialPrefabs.TaskFlow.Collections {

    public class LinkedList<T0> : IEnumerable<LinkedList<T0>.Node<T0>> where T0 : unmanaged, IEquatable<T0> {

        public struct Node<T1> : IEquatable<Node<T1>> where T1 : unmanaged, IEquatable<T1> {
            internal short Previous;
            internal short Next;
            public T1 Value;

            public readonly bool Equals(Node<T1> other) {
                return other.Previous == Previous && other.Next == Next && other.Value.Equals(Value);
            }
        }

        public struct Enumerator<T1> : IEnumerator<Node<T1>> where T1 : unmanaged, IEquatable<T1> {
            public short[] UsedList;
            public Node<T1>[] Nodes;
            public int Index;
            public int Length;

            public Node<T1> Current => Nodes[UsedList[Index]];

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext() {
                return ++Index < Length;
            }

            public void Reset() {
                Index = -1;
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
                Next = -1,
                Value = item,
            };

            Head = freeIndex;
            Tail = freeIndex;
        }

        public void Append(T0 item) {
            if (UseList.Count == 0) {
                AddFirst(item);
            } else {
                var last = UseList[^1];
                ref var lastNode = ref Nodes[last];

                var nextFree = FreeList[0];
                FreeList.RemoveAtSwapback(0);
                UseList.Add(nextFree);
                var node = new Node<T0> {
                    Next = -1,
                    Value = item,
                    Previous = last
                };
                lastNode.Next = nextFree;
                Nodes[nextFree] = node;
                Tail = nextFree;
            }
        }

        public void Remove(T0 value) {
            foreach (var element in UseList) {
                var nodeToRemove = Nodes[element];
                if (nodeToRemove.Value.Equals(value)) {
                    // We found the element, so we need to remove it and link up the neighboring nodes together
                    // If we have a previous node, then we have to link it to the next node.
                    if (nodeToRemove.Previous != -1) {
                        // Get the previous node
                        ref var prevNode = ref Nodes[nodeToRemove.Previous];
                        prevNode.Next = nodeToRemove.Next;
                    }

                    // If we have a next node, then we have to link it to the current node's previous.
                    if (nodeToRemove.Next != -1) {
                        ref var nextNode = ref Nodes[nodeToRemove.Next];
                        nextNode.Previous = nodeToRemove.Previous;
                    }

                    // TODO: Update the head and tail when we remove nodes.
                    break;
                }
            }
        }

        public IEnumerator<Node<T0>> GetEnumerator() {
            var nodes = Nodes;
            return new Enumerator<T0> {
                Nodes = nodes,
                Length = Count,
                Index = -1,
                UsedList = UseList.Collection
            };
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}

