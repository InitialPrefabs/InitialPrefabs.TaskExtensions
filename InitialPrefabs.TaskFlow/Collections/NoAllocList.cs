using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow.Collections {

    public ref struct NoAllocEnumerator<T> {
        internal Span<T> Ptr;
        internal int Index;
        internal int Length;
        public readonly T Current => Ptr[Index];

        public bool MoveNext() {
            return ++Index < Length;
        }

        public void Reset() {
            Index = -1;
        }
    }

    public ref struct NoAllocList<T> {

        internal readonly Span<T> Span;
        public readonly int Length;
        internal int Count;

        public NoAllocList(Span<T> span) {
            Span = span;
            Length = span.Length;
            Count = 0;
        }

        public NoAllocList(Span<T> span, int count) {
            Span = span;
            Length = span.Length;
            Count = count;
        }

        public void Add(T item) {
            if (Count >= Length) {
                return;
            }
            Span[Count++] = item;
        }

        public readonly NoAllocEnumerator<T> GetEnumerator() {
            return new NoAllocEnumerator<T> {
                Ptr = Span,
                Index = -1,
                Length = Count
            };
        }

        public readonly T this[int i] {
            get => Span[i];
            set => Span[i] = value;
        }
    }

    public static class NoAllocListExtensions {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(this ref NoAllocList<T> list, T item) where T : unmanaged {
            if (list.Count >= list.Length) {
                return;
            }
            list.Span[list.Count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAtSwapback<T>(this ref NoAllocList<T> list, int index) where T : unmanaged {
            list.Count--;
            var last = list.Span[list.Count];
            list.Span[index] = last;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAt<T>(this ref NoAllocList<T> list, int index) where T : unmanaged {
            list.Count--;
            for (var i = index; i < list.Count; i++) {
                list.Span[i] = list.Span[i + 1];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAt<T>(this ref NoAllocList<T> list, int index) where T : unmanaged {
            return ref list.Span[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in NoAllocList<T> list, T item) where T : unmanaged, IEquatable<T> {
            for (var i = 0; i < list.Length; i++) {
                var element = list[i];
                if (element.Equals(item)) {
                    return i;
                }
            }

            return -1;
        }
    }
}

