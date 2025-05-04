namespace InitialPrefabs.TaskFlow.Threading {

    public static class TaskGraphManager {

        internal static TaskGraph Default;
        internal static ushort UniqueID;

        public static TaskGraph Initialize(int capacity = 5) {
            Default = new TaskGraph(capacity);
            UniqueID = 0;
            return Default;
        }

        public static void Shutdown() {
            Default.Reset();
            Default = null;
            UniqueID = 0;
        }
    }
}
