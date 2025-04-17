using InitialPrefabs.TaskFlow.Collections;

namespace InitialPrefabs.TaskFlow.Threading {
    public class TaskGraphManager {

        public static readonly DynamicArray<TaskGraph> TaskGraphs;
        public static readonly DynamicArray<int> TaskHandleMetadata;

        static TaskGraphManager() {
            TaskGraphs = new DynamicArray<TaskGraph>(1) {
                new TaskGraph(5)
            };

            TaskHandleMetadata = new DynamicArray<int>(1) {
                0
            };
        }

        public TaskGraphManager() {

        }
    }
}


