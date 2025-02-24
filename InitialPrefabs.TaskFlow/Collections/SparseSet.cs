using System;
using System.Collections;
using System.Collections.Generic;

namespace InitialPrefabs.TaskFlow.Collections {

    public class FixedPool<T> {
        private readonly DynamicArray<T> data;
        private readonly DynamicArray<ushort> freeIndices;

        public FixedPool(int capacity, T defaultValue) {
            data = new DynamicArray<T>(capacity);
            freeIndices = new DynamicArray<ushort>(capacity);

            for (ushort i = 0; i < capacity; i++) {
                data.Add(defaultValue);
                freeIndices.Add(i);
            }
        }

        public (int handle, T value) Rent() {
            if (freeIndices.Count > 0) {
                var last = freeIndices.Count - 1;
                var freeIndex = freeIndices[last];
                freeIndices.RemoveAtSwapback(last);
                return (freeIndex, data[last]);
            } else {
                throw new InvalidOperationException("Exceeded rentable capacity, you need to allocate a new buffer or return an existing element.");
            }
        }
    }

    public class SparseSet<T> : IEnumerable<T> {

        private readonly DynamicArray<T> dense;
        private readonly DynamicArray<int> sparse;
        private readonly DynamicArray<int> denseToSparse;
        private readonly DynamicArray<int> freeIndices;

        public int Count => dense.Count;

        public SparseSet(int capacity) {
            dense = new DynamicArray<T>(capacity);
            sparse = new DynamicArray<int>(capacity, -1);
            denseToSparse = new DynamicArray<int>(capacity, -1);
        }

        public bool Contains(int key) {
            return key < Count && sparse[key] != -1;
        }

        private void EnsureCapacity(int key) {
            while (sparse.Count <= key) {
                sparse.Add(-1);
            }
        }

        public bool TryGet(int id, out T value) {
            if (!Contains(id)) {
                value = default;
                return false;
            }

            value = dense[sparse[id]];
            return true;
        }

        public void Add(int key, T value) {
            EnsureCapacity(key);
            if (Contains(key)) {
                return;
            }

            sparse[key] = dense.Count;
            denseToSparse.Add(key);
            dense.Add(value);
        }

        public IEnumerator<T> GetEnumerator() {
            return dense.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
