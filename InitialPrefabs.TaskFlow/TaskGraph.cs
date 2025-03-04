using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow {

    public class TaskGraph {

        // Stores an ordered list of TaskHandles
        internal DynamicArray<INode<ushort>> Nodes;
        internal DynamicArray<INode<ushort>> Sorted;

        private const int MaxTasks = 256;

        public TaskGraph(int capacity) {
            Nodes = new DynamicArray<INode<ushort>>(capacity);
            Sorted = new DynamicArray<INode<ushort>>(capacity);
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

            var taskCount = Nodes.Count;

            // Initialize the task index map
            for (var i = 0; i < taskCount; i++) {
                taskIndexMap[i] = Nodes[i].GlobalID;
            }

            for (var i = 0; i < taskCount; i++) {
                var task = Nodes[i];

                foreach (var parentID in task.GetDependencies()) {
                    var parentIdx = taskIndexMap.IndexOf(parentID, taskCount);
                    var childIdx = i;

                    if (parentIdx > -1) {
                        adjacencyMatrix[(parentIdx * MaxTasks) + childIdx] = 1;
                        inDegree[childIdx]++;
                    }
                }
            }

            PrintAdjacencyMatrix(adjacencyMatrix, taskCount);

            Span<ushort> _queue = stackalloc ushort[MaxTasks];
            var queue = new NoAllocQueue<ushort>(_queue);
            for (ushort i = 0; i < taskCount; i++) {
                if (inDegree[i] == 0) {
                    _ = queue.TryEnqueue(i);
                }
            }

            while (queue.Count > 0) {
                var taskIdx = queue.Dequeue();
                Sorted.Add(Nodes[taskIdx]);
                for (ushort x = 0; x < taskCount; x++) {
                    if (adjacencyMatrix[(taskIdx * MaxTasks) + x] == 1) {
                        inDegree[x]--;

                        if (inDegree[x] == 0) {
                            _ = queue.TryEnqueue(x);
                        }
                    }
                }
            }

            if (Sorted.Count != taskCount) {
                throw new InvalidOperationException("Cyclic dependencies occurred!");
            }
        }

        public static void PrintAdjacencyMatrix(Span<ushort> adjacencyMatrix, int taskCount) {
            Console.Write("    ");
            for (var col = 0; col < taskCount; col++) {
                Console.Write($"{col,3} ");
            }
            Console.WriteLine();
            Console.WriteLine("   +" + new string('-', taskCount * 4));

            for (var row = 0; row < taskCount; row++) {
                Console.Write($"{row,2} |"); // Row index
                for (var col = 0; col < taskCount; col++) {
                    var index = (row * taskCount) + col; // Convert 2D index to 1D
                    Console.Write($" {adjacencyMatrix[index],2} ");
                }
                Console.WriteLine();
            }
        }

    }
}
