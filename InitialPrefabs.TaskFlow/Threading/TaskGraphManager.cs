namespace InitialPrefabs.TaskFlow.Threading {

    public static class TaskGraphRunner {

        internal delegate void ResetHandler();

        public static TaskGraph Graph => Default;
        private static ResetHandler ResetEvent;

        internal static event ResetHandler OnReset {
            add {
                ResetEvent -= value;
                ResetEvent += value;
            }
            remove =>
                ResetEvent -= value;
        }

        internal static TaskGraph Default;
        internal static ushort UniqueID;

        public static void Reset() {
            UniqueID = 0;
            Default.Reset();
            ResetEvent?.Invoke();
        }

        public static void Update() {
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
