using System;
using System.Collections;
using System.Collections.Generic;

namespace InitialPrefabs.TaskExtensions {
    public class DynamicArray<T> : IEnumerable<T> {
        internal T[] Collection;

        public int Capacity => Collection.Length;
        public int Count => count;

        private int count;

        public DynamicArray(int capacity) {
            Collection = new T[capacity];
            count = 0;
        }

        public T this[int i] {
            get => Collection[i];
            set => Collection[i] = value;
        }

        public ReadOnlySpan<T> AsReadOnlySpan() {
            return new ReadOnlySpan<T>(Collection, 0, Count);
        }

        public void Push(T value) {
            if (count >= Capacity) {
                Array.Resize(ref Collection, Capacity + 1);
            }
            Collection[count++] = value;
        }

        public void RemoveAtSwapback(int index) {
            count--;
            var last = Collection[count];
            Collection[index] = last;
        }

        public void RemoveAt(int index) {
            count--;
            for (var i = index; i < count; i++) {
                Collection[i] = Collection[i + 1];
            }
        }

        public void Resize(int capacity) {
            Array.Resize(ref Collection, capacity);
        }

        public IEnumerator<T> GetEnumerator() {
            for (int i = 0; i < Count; i++) {
                yield return Collection[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
