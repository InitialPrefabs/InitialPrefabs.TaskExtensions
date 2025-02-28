using InitialPrefabs.TaskFlow.Collections;

namespace InitialPrefabs.TaskFlow {

    // TODO: Implement topological sorting https://en.wikipedia.org/wiki/Topological_sorting
    public class TaskGraph {

        internal DynamicArray<INode<ushort>> BoxedTrackables;

        public TaskGraph(int capacity) {
            BoxedTrackables = new DynamicArray<INode<ushort>>(capacity);
        }

        public void Track(INode<ushort> trackedTask) {
            throw new System.NotImplementedException();
        }
    }
}
