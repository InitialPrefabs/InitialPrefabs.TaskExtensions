using InitialPrefabs.TaskFlow.Collections;

namespace InitialPrefabs.TaskFlow.Threading {

    public struct GraphHandle {
        internal ushort Index;

        public GraphHandle(ushort index) {
            this.Index = index;
        }

        public static implicit operator ushort(GraphHandle value) {
            return value.Index;
        }
    }

    public static class TaskGraphManager {

        public static readonly DynamicArray<TaskGraph> TaskGraphs;
        public static readonly DynamicArray<ushort> TaskHandleMetadata;
        public static readonly DynamicArray<GraphHandle> Handles;

        static TaskGraphManager() {
            TaskGraphs = new DynamicArray<TaskGraph>(1) {
                new TaskGraph(5)
            };

            TaskHandleMetadata = new DynamicArray<ushort>(1) {
                0
            };

            Handles = new DynamicArray<GraphHandle>(1) {
                new GraphHandle(0)
            };
        }

        public static UnmanagedRef<GraphHandle> CreateGraph(int capacity = 5) {
            TaskGraphs.Add(new TaskGraph(capacity));
            TaskHandleMetadata.Add(0);
            Handles.Add(new GraphHandle((ushort)TaskGraphs.Count));
            ref var e = ref Handles.ElementAt(Handles.Count - 1);
            return new UnmanagedRef<GraphHandle>(ref e);
        }

        public static void RemoveGraph(GraphHandle handle) {
            TaskGraphs.RemoveAt(handle);
            TaskHandleMetadata.RemoveAt(handle);
            Handles.RemoveAt(handle);

            for (ushort i = 0; i < Handles.Count; i++) {
                ref var graphHandle = ref Handles.ElementAt(i);
                graphHandle.Index = i;
            }
        }

        public static void Reset(GraphHandle handle) {
            TaskGraphs[handle].Reset();
            TaskHandleMetadata[handle] = 0;
        }

        public static (TaskGraph graph, UnmanagedRef<ushort> uniqueId) Get(GraphHandle handle) {
            ref var metadata = ref TaskHandleMetadata.ElementAt(handle);
            return (TaskGraphs[handle], new UnmanagedRef<ushort>(ref metadata));
        }
    }
}

