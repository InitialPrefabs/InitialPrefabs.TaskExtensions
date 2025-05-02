using InitialPrefabs.TaskFlow.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Collections {

    public enum ResizeType {
        /// <summary>
        /// The <see cref="DynamicArray{T}.Count"/> does not grow or shrink when
        /// forcibly resized.
        /// </summary>
        LeaveCountAsIs,
        /// <summary>
        /// Forces the <see cref="DynamicArray{T}"/> to match the new capacity.
        /// </summary>
        ForceCount,

        /// <summary>
        /// Forces the <see cref="DynamicArray{T}.Count"/> to be 0. This is effectively
        /// a <see cref="DynamicArray{T}.Clear()"/>..
        /// </summary>
        ResetCount,
    }

    /// <summary>
    /// Similar to a <see cref="List{T}"/> with an internal array. This avoids having to
    /// construct an array via a list as we can access the <see cref="Collection"/>.
    /// </summary>
    public class DynamicArray<T0> : IEnumerable<T0>, IReadOnlyList<T0> {

        internal T0[] Collection;

        public int Capacity => Collection.Length;
        public bool IsEmpty => Count == 0;

        public int Count => count;
        internal int count;

        public DynamicArray(int capacity) {
            Collection = new T0[capacity];
            count = 0;
        }

        public DynamicArray(int capacity, T0 defaultValue) : this(capacity) {
            foreach (ref var element in new Span<T0>(Collection)) {
                element = defaultValue;
            }
        }

        public T0 this[int i] {
            get => Collection[i];
            set => Collection[i] = value;
        }

        public ReadOnlySpan<T0> AsReadOnlySpan() {
            return new ReadOnlySpan<T0>(Collection, 0, Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            count = 0;
        }

        public void AsyncAdd(T0 value) {
            if (Count >= Capacity) {
                throw new InvalidOperationException(
                    "Cannot add value to the internal buffer due to maximizing capacity.");
            }
            Collection[Interlocked.Increment(ref count) - 1] = value;
        }

        public void Add(T0 value) {
            if (Count >= Capacity) {
                ForceResize(Count + 1);
            }
            Collection[count++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapback(int index) {
            count--;
            var last = Collection[Count];
            Collection[index] = last;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index) {
            for (var i = index; i < Count; i++) {
                Collection[i] = Collection[MathUtils.Clamp(i + 1, Count)];
            }
            count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T0 ElementAt(int i) {
            return ref Collection[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceResize(int capacity, ResizeType resizeType = ResizeType.LeaveCountAsIs) {
            var array = new T0[capacity];
            var length = MathUtils.Min(Collection.Length, capacity);
            Array.Copy(Collection, array, length);
            Collection = array;

            count = resizeType switch {
                ResizeType.ForceCount => capacity,
                ResizeType.ResetCount => 0,
                ResizeType.LeaveCountAsIs => Count,
                _ => Count
            };
        }

        public int IndexOf<T1>(T0 element, T1 comparer) where T1 : IComparer<T0> {
            for (var i = 0; i < Count; i++) {
                if (comparer.Compare(element, Collection[i]) == 0) {
                    return i;
                }
            }
            return -1;
        }

        public int Find(Predicate<T0> predicate) {
            for (var i = 0; i < Count; i++) {
                var element = Collection[i];
                if (predicate.Invoke(element)) {
                    return i;
                }
            }
            return -1;
        }

        public IEnumerator<T0> GetEnumerator() {
            for (var i = 0; i < Count; i++) {
                yield return Collection[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
