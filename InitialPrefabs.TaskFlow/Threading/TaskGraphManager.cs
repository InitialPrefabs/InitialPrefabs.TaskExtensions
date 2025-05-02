using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public struct GraphHandle : IEquatable<GraphHandle> {
        internal ushort Index;

        public GraphHandle(ushort index) {
            Index = index;
        }

        public bool Equals(GraphHandle other) {
            return other.Index == Index;
        }

        public static implicit operator ushort(GraphHandle value) {
            return value.Index;
        }
    }

    public static class TaskGraphManager {

        internal static readonly DynamicArray<TaskGraph> TaskGraphs = new DynamicArray<TaskGraph>(1);
        internal static readonly DynamicArray<ushort> TaskHandleMetadata = new DynamicArray<ushort>(1);
        internal static readonly DynamicArray<GraphHandle> Handles = new DynamicArray<GraphHandle>(1);

        public static void Initialize(int graphCapacity) {
            Shutdown();
            _ = CreateGraph(graphCapacity);
        }

        public static void Shutdown() {
            foreach (var graph in TaskGraphs) {
                graph.Reset();
            }

            TaskGraphs.Clear();
            TaskHandleMetadata.Clear();
            Handles.Clear();
        }

        public static UnmanagedRef<GraphHandle> CreateGraph(int capacity = 5) {
            Handles.Add(new GraphHandle((ushort)TaskGraphs.Count));
            TaskGraphs.Add(new TaskGraph(capacity));
            TaskHandleMetadata.Add(0);
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

        public static (TaskGraph graph, UnmanagedRef<ushort> localGraphId) Get() {
            return Get(default);
        }

        public static (TaskGraph graph, UnmanagedRef<ushort> localGraphId) Get(GraphHandle handle) {
            ref var metadata = ref TaskHandleMetadata.ElementAt(handle);
            return (TaskGraphs[handle], new UnmanagedRef<ushort>(ref metadata));
        }
    }
}

