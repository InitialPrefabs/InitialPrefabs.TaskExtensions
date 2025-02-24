namespace InitialPrefabs.TaskFlow.Collections {
    public class FixedPool<T> {
        private readonly DynamicArray<T> data;
        private readonly DynamicArray<ushort> freeIndices;

        public int RemainingFreeCount => freeIndices.Count;

        public FixedPool(int capacity, T defaultValue) {
            data = new DynamicArray<T>(capacity);
            freeIndices = new DynamicArray<ushort>(capacity);

            for (ushort i = 0; i < capacity; i++) {
                data.Add(defaultValue);
                freeIndices.Add(i);
            }
        }

        public bool TryRent(out (ushort handle, T value) rented) {
            if (freeIndices.Count > 0) {
                var last = freeIndices.Count - 1;
                var freeIndex = freeIndices[last];
                freeIndices.RemoveAtSwapback(last);
                rented = (freeIndex, data[last]);
                return true;
            } else {
                rented = default;
                return false;
            }
        }

        public void Return(ushort handle) {
            freeIndices.Add(handle);
        }

        public void Reserve(T value) {
            if (freeIndices.Count == 0) {
                data.Add(value);
                freeIndices.Add((ushort)(data.Count - 1));
            }
        }
    }
}

