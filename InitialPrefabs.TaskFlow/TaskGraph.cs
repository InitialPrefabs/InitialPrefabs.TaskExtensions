using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow {

    // TODO: Implement topological sorting https://en.wikipedia.org/wiki/Topological_sorting
    public class TaskGraph {

        // Stores an ordered list of TaskHandles
        internal DynamicArray<INode<ushort>> Nodes;

        [Obsolete]
        internal DynamicArray<INode<ushort>> Sorted;

        [Obsolete]
        internal DynamicArray<INode<ushort>> Independent;

        private const int MaxTasks = 256;

        public TaskGraph(int capacity) {
            Nodes = new DynamicArray<INode<ushort>>(capacity);
            // Sorted = new DynamicArray<INode<ushort>>(capacity);
            // Independent = new DynamicArray<INode<ushort>>(capacity);
        }

        public void Track(INode<ushort> trackedTask) {
            // TODO: Check if the dependency already exists
            Nodes.Add(trackedTask);
        }

        internal bool IsDependent(int index) {
            for (var i = 0; i < Nodes.Count; i++) {
                if (i == index) {
                    continue;
                }

                var node = Nodes[i];
                foreach (ref readonly var dependency in node.GetDependencies()) {
                    if (dependency == index) {
                        return true;
                    }
                }
            }
            return false;
        }

        internal void Sort() {
            if (Nodes.Count == 0) {
                return;
            }


            // TODO: Maybe preallocate onto the heap a max task pool.
            Span<int> inDegree = stackalloc int[MaxTasks];
            Span<byte> _internalBytes = stackalloc byte[MaxTasks / 4];
            var visited = new NoAllocBitArray(_internalBytes);
            Span<ushort> adjacencyMatrix = stackalloc ushort[MaxTasks * MaxTasks];
            Span<ushort> taskIndexMap = stackalloc ushort[MaxTasks];

            for (var i = 0; i < Nodes.Count; i++) {
                taskIndexMap[i] = Nodes[i].GlobalID;
            }
        }
    }
}
