using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    public static class TaskConstants {
        internal const int MaxTasks = 256;
    }

    internal static class TaskGraphExtensions {
        public static Span<sbyte> AsSpan(this ref TaskGraph._Edges matrix) {
            unsafe {
                fixed (sbyte* ptr = matrix.Data) {
                    return new Span<sbyte>(ptr, TaskConstants.MaxTasks);
                }
            }
        }

        public static void Reset(this ref TaskGraph._Edges matrix) {
            var span = matrix.AsSpan();
            foreach (ref var element in span) {
                element = default;
            }
        }
    }

    public class TaskGraph {

        internal unsafe struct _Edges {
            public fixed sbyte Data[TaskConstants.MaxTasks];
        }

        internal unsafe struct _TaskBuffer {
            public fixed byte Data[TaskConstants.MaxTasks];

            public readonly Span<byte> AsSpan() {
                fixed (byte* ptr = Data) {
                    return new Span<byte>(ptr, TaskConstants.MaxTasks);
                }
            }
        }

        internal unsafe struct MaxByteBools {
            internal fixed byte Data[TaskConstants.MaxTasks / 4];

            public readonly Span<byte> AsByteSpan() {
                fixed (byte* ptr = Data) {
                    return new Span<byte>(ptr, TaskConstants.MaxTasks / 4);
                }
            }
        }

        // Stores an ordered list of TaskHandles
        internal DynamicArray<INode<ushort>> Nodes;
        internal DynamicArray<(INode<ushort> node, UnmanagedRef<TaskMetadata> metadata)> Sorted;
        internal DynamicArray<TaskMetadata> Metadata; // TODO: Sort the metadata also?
        internal MaxByteBools Bytes;
        internal _Edges Edges;

        // Task Queue section
        internal ConcurrentQueue<(INode<ushort> node, UnmanagedRef<TaskMetadata> metadata)> taskQueue;
        internal int RunningTasks;

        internal TaskWorker[] Workers;
        internal _TaskBuffer Free;
        internal _TaskBuffer Used;

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
            Edges = default;
        }

        // TODO: Write an allocation strategy
        public TaskGraph(int capacity) {
            Nodes = new DynamicArray<INode<ushort>>(capacity);
            Sorted = new DynamicArray<(INode<ushort>, UnmanagedRef<TaskMetadata>)>(capacity);
            Metadata = new DynamicArray<TaskMetadata>(capacity);

            for (var i = 0; i < capacity; i++) {
                Metadata.Add(new TaskMetadata());
            }
            Metadata.Clear();

            Workers = new TaskWorker[TaskConstants.MaxTasks];
            var freeList = new NoAllocList<byte>(Free.AsSpan());
            for (var i = 0; i < capacity; i++) {
                Workers[i] = new TaskWorker();
                freeList.Add((byte)i);
            }
        }

        internal WorkerHandle RequestWorker() {
            var freeList = new NoAllocList<byte>(Free.AsSpan());
            var useList = new NoAllocList<byte>(Used.AsSpan());
            var freeWorker = freeList[0];
            useList.Add(freeWorker);
            return new WorkerHandle(freeWorker);
        }

        internal void ReturnWorker(WorkerHandle handle) {
            // var freeList = new NoAllocList<WorkerHandle>(Free.AsSpan());
            // var useList = new NoAllocList<byte>(Used.AsSpan());
            // var idx = useList.IndexOf
        }

        public void Track(INode<ushort> trackedTask, TaskWorkload workload) {
            var span = Bytes.AsByteSpan();
            var bitArray = new NoAllocBitArray(span);

            if (!bitArray[trackedTask.GlobalID]) {
                if (Nodes.Count >= Metadata.Capacity) {
                    // We need to add a default metadata
                    var metadata = TaskMetadata.Default();
                    metadata.Workload = workload;
                    Metadata.Add(metadata);
                } else {
                    Metadata.count++;
                    ref var metadata = ref Metadata.ElementAt(Nodes.Count);
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
            Span<byte> _internalBytes = stackalloc byte[NoAllocBitArray.CalculateSize(
                TaskConstants.MaxTasks)];
            var visited = new NoAllocBitArray(_internalBytes);
            Span<byte> adjacencyMatrix = stackalloc byte[
                TaskConstants.MaxTasks * TaskConstants.MaxTasks];

            var taskCount = Nodes.Count;

            for (var i = 0; i < taskCount; i++) {
                var task = Nodes[i];

                foreach (var parentID in task.GetDependencies()) {
                    var parentIdx = Nodes.Find(element => element.GlobalID == parentID);
                    var childIdx = i;

                    if (parentIdx > -1) {
                        adjacencyMatrix[(parentIdx * TaskConstants.MaxTasks) + childIdx] = 1;
                        inDegree[childIdx]++;
                    }
                }
            }

            PrintAdjacencyMatrix(adjacencyMatrix, taskCount);

            Span<ushort> _queue = stackalloc ushort[TaskConstants.MaxTasks];
            var queue = new NoAllocQueue<ushort>(_queue);
            for (ushort i = 0; i < taskCount; i++) {
                if (inDegree[i] == 0) {
                    _ = queue.TryEnqueue(i);
                }
            }

            while (queue.Count > 0) {
                var taskIdx = queue.Dequeue();
                Sorted.Add((Nodes[taskIdx],
                    new UnmanagedRef<TaskMetadata>(ref Metadata.ElementAt(taskIdx))));
                for (ushort x = 0; x < taskCount; x++) {
                    if (adjacencyMatrix[(taskIdx * TaskConstants.MaxTasks) + x] == 1) {
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
                var element = Sorted[i];

                if (inDegree[i] == 0) {
                    _ = Interlocked.Increment(ref RunningTasks);
                    // TODO: Queue the tasks
                    taskQueue.Enqueue(element);
                }
            }

            while (RunningTasks > 0) {
                if (taskQueue.TryDequeue(out var element)) {
                    // We have to dequeue the task. Figure out how many Workers we need to spawn
                    var workload = element.metadata.Ref.Workload;
                    var task = element.node.Task;

                    switch (workload.Type) {
                        case WorkloadType.Fake:
                            break;
                        case WorkloadType.SingleThreadNoLoop: {
                                // Create an action
                                Action action = () => {
                                    task.Execute(-1);
                                };

                                // Launch a worker
                                break;
                            }
                        case WorkloadType.SingleThreadLoop: {
                                Action action = () => {
                                    var metadata = element.metadata;
                                    for (var i = 0; i < workload.Length && !metadata.Ref.Token.IsCancellationRequested; i++) {
                                        task.Execute(i);
                                    }
                                };
                                break;
                            }
                        case WorkloadType.MultiThreadLoop: {
                                for (var x = 0; x < workload.ThreadCount; x++) {
                                    // Determine the slice
                                    var startOffset = x * workload.BatchSize;
                                    var diff = workload.Total - startOffset;
                                    var length = diff > workload.BatchSize ? workload.BatchSize : diff;

                                    Action action = () => {
                                        var metadata = element.metadata;
                                        for (var i = 0; i < length && !metadata.Ref.Token.IsCancellationRequested; i++) {
                                            var idx = startOffset + i;
                                            task.Execute(idx);
                                        }
                                    };

                                    // Now create a unit task
                                }
                                break;
                            }
                    }
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
