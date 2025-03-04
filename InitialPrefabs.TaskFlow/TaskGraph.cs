using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Diagnostics;

namespace InitialPrefabs.TaskFlow {

    public class TaskGraph {
        private const int MaxTasks = 256;

        internal unsafe struct MaxBytes {
            internal fixed byte Data[MaxTasks / 4];

            public readonly Span<byte> AsByteSpan() {
                fixed (byte* ptr = Data) {
                    return new Span<byte>(ptr, MaxTasks / 4);
                }
            }
        }

        // Stores an ordered list of TaskHandles
        internal DynamicArray<INode<ushort>> Nodes;
        internal DynamicArray<INode<ushort>> Sorted;
        internal MaxBytes Bytes;

        public void Reset() {
            foreach (var node in Nodes) {
                node.Dispose();
            }
            Nodes.Clear();
            Sorted.Clear();
            Bytes = default;
        }

        public TaskGraph(int capacity) {
            Nodes = new DynamicArray<INode<ushort>>(capacity);
            Sorted = new DynamicArray<INode<ushort>>(capacity);
        }

        public void Track(INode<ushort> trackedTask) {
            var span = Bytes.AsByteSpan();
            var bitArray = new NoAllocBitArray(span);

            if (!bitArray[trackedTask.GlobalID]) {
                bitArray[trackedTask.GlobalID] = true;
                Nodes.Add(trackedTask);
            }
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

            Span<int> inDegree = stackalloc int[MaxTasks];
            Span<byte> _internalBytes = stackalloc byte[NoAllocBitArray.CalculateSize(MaxTasks)];
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

            Console.WriteLine($"Sorted: {Sorted.Count}, Task Count: {taskCount}");
            if (Sorted.Count != taskCount) {
                throw new InvalidOperationException("Cyclic dependencies occurred, aborting!");
            }
        }

        [Conditional("DEBUG")]
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
