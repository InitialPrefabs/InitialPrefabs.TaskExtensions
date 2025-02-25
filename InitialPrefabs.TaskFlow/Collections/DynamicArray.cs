using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow.Collections {

    /// <summary>
    /// Similar to a <see cref="List{T}"/> with an internal array. This avoids having to
    /// construct an array via a list as we can access the <see cref="Collection"/>.
    /// </summary>
    public class DynamicArray<T> : IEnumerable<T>, IReadOnlyList<T> {
        internal T[] Collection;

        public int Capacity => Collection.Length;
        public bool IsEmpty => Count == 0;

        public int Count { get; internal set; }

        public DynamicArray(int capacity) {
            Collection = new T[capacity];
            Count = 0;
        }

        public DynamicArray(int capacity, T defaultValue) : this(capacity) {
            foreach (ref var element in new Span<T>(Collection)) {
                element = defaultValue;
            }
        }

        public T this[int i] {
            get => Collection[i];
            set => Collection[i] = value;
        }

        public ReadOnlySpan<T> AsReadOnlySpan() {
            return new ReadOnlySpan<T>(Collection, 0, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Count = 0;
        }

        public void Add(T value) {
            if (Count >= Capacity) {
                Array.Resize(ref Collection, Capacity + 1);
            }
            Collection[Count++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapback(int index) {
            Count--;
            var last = Collection[Count];
            Collection[index] = last;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index) {
            Count--;
            for (var i = index; i < Count; i++) {
                Collection[i] = Collection[i + 1];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceResize(int capacity) {
            Collection = new T[capacity];
        }

        public IEnumerator<T> GetEnumerator() {
            for (var i = 0; i < Count; i++) {
                yield return Collection[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
