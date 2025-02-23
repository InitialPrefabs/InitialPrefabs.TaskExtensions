using System.Collections;
using System.Collections.Generic;

namespace InitialPrefabs.TaskFlow.Collections {

    public class SparseSet<T> : IEnumerable<T> {

        private readonly DynamicArray<T> dense;
        private readonly DynamicArray<int> sparse;
        private readonly DynamicArray<int> denseToSparse;

        public int Count => dense.Count;

        public SparseSet(int capacity) {
            dense = new DynamicArray<T>(capacity);
            sparse = new DynamicArray<int>(capacity, -1);
            denseToSparse = new DynamicArray<int>(capacity, -1);
        }

        public bool Contains(int key) {
            return key < Count && sparse[key] != -1;
        }

        private void EnsureCapacity(int id) {
            while (sparse.Count <= id) {
                sparse.Push(-1);
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
            if (Contains(key)) {
                return;
            }

            sparse[key] = dense.Count;
            denseToSparse.Push(key);
            dense.Push(value);
        }

        public IEnumerator<T> GetEnumerator() {
            return dense.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
