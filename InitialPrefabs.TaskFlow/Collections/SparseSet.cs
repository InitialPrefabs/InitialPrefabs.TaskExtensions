namespace InitialPrefabs.TaskFlow.Collections {
    public class SparseSet<T> {

        private readonly DynamicArray<T> dense;
        private readonly DynamicArray<int> sparse;
        private readonly DynamicArray<int> reverseLookup;

        public SparseSet(int capacity) {
            dense = new DynamicArray<T>(capacity);
            sparse = new DynamicArray<int>(capacity);
            reverseLookup = new DynamicArray<int>(capacity);
        }
    }
}
