using System;

namespace InitialPrefabs.TaskFlow.Collections {

    public ref struct NoAllocQueue<T> where T : IEquatable<T> {
        internal readonly Span<T> Ptr;
        internal int Head;
        internal int Tail;
        public readonly int Capacity;

        public int Count { get; internal set; }
        public bool IsEmpty => Count == 0;

        public NoAllocQueue(Span<T> span) {
            Ptr = span;
            Capacity = span.Length;
            Head = 0;
            Tail = 0;
            Count = 0;
        }
    }

    public static class NoAllocQueueExtensions {

        public static bool TryPeek<T>(this ref NoAllocQueue<T> queue, out T item) where T : IEquatable<T> {
            if (queue.Count > 0) {
                item = queue.Peek();
                return true;
            }

            item = default;
            return false;
        }

        public static T Peek<T>(this ref NoAllocQueue<T> queue) where T : IEquatable<T> {
            return queue.Ptr[queue.Head];
        }

        public static bool TryEnqueue<T>(this ref NoAllocQueue<T> queue, T item) where T : IEquatable<T> {
            if (queue.Count == queue.Capacity) {
                return false;
            }

            queue.Ptr[queue.Tail] = item;
            queue.Tail = (queue.Tail + 1) % queue.Capacity;
            queue.Count++;
            return true;
        }

        public static bool TryDequeue<T>(this ref NoAllocQueue<T> queue, out T item) where T : IEquatable<T> {
            if (queue.Count > 0) {
                item = queue.Dequeue();
                return true;
            }

            item = default;
            return false;
        }

        internal static T Dequeue<T>(this ref NoAllocQueue<T> queue) where T : IEquatable<T> {
            var element = queue.Ptr[queue.Head];
            queue.Head = (queue.Head + 1) % queue.Capacity;
            queue.Count--;
            return element;
        }

        public static bool Contains<T>(this ref NoAllocQueue<T> queue, in T item) where T : IEquatable<T> {
            for (int i = 0, index = queue.Head; i < queue.Count; i++) {
                if (queue.Ptr[index].Equals(item)) {
                    return true;
                }
                index = (index + 1) % queue.Capacity;
            }
            return false;
        }

        public static void Clear<T>(this ref NoAllocQueue<T> queue) where T : IEquatable<T> {
            queue.Head = 0;
            queue.Tail = 0;
            queue.Count = 0;
        }
    }
}

