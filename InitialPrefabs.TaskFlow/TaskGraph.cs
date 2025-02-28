using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow {

    // TODO: Implement topological sorting https://en.wikipedia.org/wiki/Topological_sorting
    public class TaskGraph {

        // Stores an ordered list of TaskHandles
        internal DynamicArray<INode<ushort>> BoxedTrackables;
        internal DynamicArray<INode<ushort>> Sorted;
        internal DynamicArray<INode<ushort>> Independent;

        public TaskGraph(int capacity) {
            BoxedTrackables = new DynamicArray<INode<ushort>>(capacity);
            Sorted = new DynamicArray<INode<ushort>>(capacity);
            Independent = new DynamicArray<INode<ushort>>(capacity);
        }

        public void Track(INode<ushort> trackedTask) {
            throw new System.NotImplementedException();
        }

        private void Sort() {
            // First get all independent dependencies, so we need to build a map of some kind
            // The current problem right now is that, to build this sort of graph and figure out
            // standalone dependencies. I need to know which handles rely on another handle.
            // While I have the children, the node doesn't know what its parent is.
            // Handle<T0> only contains the index within a TaskUnitPool<T0>, but if there are
            // different type for TaskUnitPools, collision will occur with the IDs.
            Span<int> map = stackalloc int[BoxedTrackables.Count];

            for (var i = 0; i < BoxedTrackables.Count; i++) {
            }
        }
    }
}
