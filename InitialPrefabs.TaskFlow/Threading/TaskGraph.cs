using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    internal static class TaskGraphExtensions {
        public static Span<sbyte> AsSpan(this ref TaskGraph.Buffer matrix) {
            unsafe {
                fixed (sbyte* ptr = matrix.Data) {
                    return new Span<sbyte>(ptr, TaskGraph.MaxTasks);
                }
            }
        }

        public static void Reset(this ref TaskGraph.Buffer matrix) {
            var span = matrix.AsSpan();
            foreach (ref var element in span) {
                element = default;
            }
        }
    }

    public class TaskGraph {

        internal const int MaxTasks = 256;

        internal unsafe struct Buffer {
            public fixed sbyte Data[MaxTasks];
        }

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
        internal DynamicArray<TaskMetadata> Metadata; // TODO: Sort the metadata also?
        internal MaxBytes Bytes;
        internal Buffer Edges;

        internal ConcurrentQueue<(int sortIndex, INode<ushort> node)> queue;

        internal int RunningTasks;

        public void Reset() {
            RunningTasks = 0;

            foreach (var node in Nodes) {
                node.Dispose();
            }
            // Reset all metadata
            for (var i = 0; i < Metadata.Collection.Length; i++) {
                ref var collection = ref Metadata.Collection[i];
                collection.Reset();
            }

            Metadata.Clear();
            Nodes.Clear();
            Sorted.Clear();
            Bytes = default;
        }

        public TaskGraph(int capacity) {
            Nodes = new DynamicArray<INode<ushort>>(capacity);
            Sorted = new DynamicArray<INode<ushort>>(capacity);
            Metadata = new DynamicArray<TaskMetadata>(capacity);

            for (var i = 0; i < capacity; i++) {
                Metadata.Add(new TaskMetadata());
            }
            Metadata.Clear();
        }

        public void Track(INode<ushort> trackedTask, TaskWorkload workload) {
            var span = Bytes.AsByteSpan();
            var bitArray = new NoAllocBitArray(span);

            if (!bitArray[trackedTask.GlobalID]) {
                if (Nodes.Count >= Metadata.Capacity) {
                    // We need to add a default metadata
                    var metadata = new TaskMetadata();
                    metadata.Workload = workload;
                    Metadata.Add(metadata);
                } else {
                    Metadata.Count++;
                    var metadata = Metadata[Nodes.Count];
                    // Reset the task
                    metadata.Reset();
                    metadata.Workload = workload;
                }

                // When we track a task, the associated metadata must also be enabled
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

            Edges.Reset();
            var inDegree = Edges.AsSpan();
            Span<byte> _internalBytes = stackalloc byte[NoAllocBitArray.CalculateSize(MaxTasks)];
            var visited = new NoAllocBitArray(_internalBytes);
            Span<byte> adjacencyMatrix = stackalloc byte[MaxTasks * MaxTasks];

            var taskCount = Nodes.Count;

            for (var i = 0; i < taskCount; i++) {
                var task = Nodes[i];

                foreach (var parentID in task.GetDependencies()) {
                    var parentIdx = Nodes.Find(element => element.GlobalID == parentID);
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
                throw new InvalidOperationException("Cyclic dependencies occurred, aborting!");
            }
        }

        public void Process() {
            var inDegree = Edges.AsSpan();

            // Main thread implementation
            for (var i = 0; i < Sorted.Count; i++) {
                (int taskIndex, INode<ushort> task) node = (i, Sorted[i]);

                if (inDegree[i] == 0) {
                    _ = Interlocked.Increment(ref RunningTasks);
                    // TODO: Queue the tasks
                    queue.Enqueue(node);
                }
            }

            while (RunningTasks > 0) {
                if (queue.TryDequeue(out var data)) {
                    var metadata = Metadata[data.sortIndex];
                    var task = data.node.Task;
                    var token = metadata.Token;
                }
            }
        }

        [Conditional("DEBUG")]
        public static void PrintAdjacencyMatrix(Span<byte> adjacencyMatrix, int taskCount) {
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
