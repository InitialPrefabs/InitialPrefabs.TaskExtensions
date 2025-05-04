using System;
using System.Collections;
using System.Collections.Generic;

namespace InitialPrefabs.TaskFlow.Collections {

    public class LinkedList<T0> : IEnumerable<LinkedList<T0>.Node<T0>> where T0 : unmanaged, IEquatable<T0> {

        public struct Node<T1> : IEquatable<Node<T1>> where T1 : unmanaged, IEquatable<T1> {
            internal short PreviousIdx;
            internal short NextIdx;
            public T1 Value;

            public readonly bool Equals(Node<T1> other) {
                return other.PreviousIdx == PreviousIdx &&
                    other.NextIdx == NextIdx &&
                    other.Value.Equals(Value);
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

        public ref Node<T0> Head => ref Nodes[UseList[0]];
        public ref Node<T0> Tail => ref Nodes[UseList[UseList.Count - 1]];

        internal short HeadIndex;
        internal short TailIndex;

        public LinkedList(int capacity) {
            Nodes = new Node<T0>[capacity];
            FreeList = new DynamicArray<short>(capacity);
            UseList = new DynamicArray<short>(capacity);

            for (short i = 0; i < capacity; i++) {
                FreeList.Add(i);
            }

            HeadIndex = -1;
            TailIndex = -1;
        }

        public LinkedList(int capacity, T0 value) : this(capacity) {
            _ = AddFirst(value);
        }

        public UnmanagedRef<T0> AddFirst(T0 item) {
            // Take the first node
            var freeIndex = FreeList[0];
            FreeList.RemoveAtSwapback(0);
            UseList.Add(freeIndex);

            Nodes[freeIndex] = new Node<T0> {
                PreviousIdx = -1,
                NextIdx = -1,
                Value = item,
            };

            HeadIndex = freeIndex;
            TailIndex = freeIndex;

            return new UnmanagedRef<T0>(ref Nodes[freeIndex].Value);
        }

        public UnmanagedRef<T0> Append(T0 item) {
            if (UseList.Count == 0) {
                return AddFirst(item);
            } else {
                var last = UseList[^1];
                ref var lastNode = ref Nodes[last];

                var nextFree = FreeList[0];
                FreeList.RemoveAtSwapback(0);
                UseList.Add(nextFree);
                Nodes[nextFree] = new Node<T0> {
                    NextIdx = -1,
                    Value = item,
                    PreviousIdx = last
                };

                lastNode.NextIdx = nextFree;
                TailIndex = nextFree;
                return new UnmanagedRef<T0>(ref Nodes[nextFree].Value);
            }
        }

        public void Remove(T0 value) {
            for (var i = UseList.Count - 1; i >= 0; i--) {
                var nodeToRemove = Nodes[UseList[i]];
                if (nodeToRemove.Value.Equals(value)) {
                    // We found the element, so we need to remove it and link up the neighboring nodes together
                    // If we have a previous node, then we have to link it to the next node.
                    if (nodeToRemove.PreviousIdx != -1) {
                        // Get the previous node
                        ref var prevNode = ref Nodes[nodeToRemove.PreviousIdx];
                        prevNode.NextIdx = nodeToRemove.NextIdx;
                    } else {
                        // We know that we are removing the head, so the next value should be the head
                        HeadIndex = nodeToRemove.NextIdx;
                        ref var newHead = ref Nodes[HeadIndex];
                        newHead.PreviousIdx = -1;
                    }

                    // If we have a next node, then we have to link it to the current node's previous.
                    if (nodeToRemove.NextIdx != -1) {
                        ref var nextNode = ref Nodes[nodeToRemove.NextIdx];
                        nextNode.PreviousIdx = nodeToRemove.PreviousIdx;
                    } else {
                        TailIndex = nodeToRemove.PreviousIdx;
                        ref var newTail = ref Nodes[nodeToRemove.PreviousIdx];
                        newTail.NextIdx = -1;
                    }
                    // TODO: Update the head and tail when we remove nodes.
                    UseList.RemoveAt(i);
                    break;
                }
            }
        }

        public ref Node<T0> ElementAt(int idx) {
            var remapped = UseList[idx];
            return ref Nodes[remapped];
        }

        public void Clear() {
            foreach (var element in UseList) {
                FreeList.Add(element);
            }

            UseList.Clear();
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

