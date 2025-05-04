namespace InitialPrefabs.TaskFlow.Threading {

    public static class TaskGraphManager {

        public static TaskGraph Graph => Default;

        internal static TaskGraph Default;
        internal static ushort UniqueID;

        public static TaskGraph Initialize(int capacity = 5) {
            Default = new TaskGraph(capacity);
            UniqueID = 0;
            return Default;
        }

        public static void ResetContext() {
            UniqueID = 0;
            Default.Reset();
        }

        public static void Process() {
            Default.Sort();
            Default.Process();
        }

        public static void Shutdown() {
            Default.Reset();
            Default = null;
            UniqueID = 0;
        }
    }
}
