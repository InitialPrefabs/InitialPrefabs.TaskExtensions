using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Diagnostics;

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

        internal unsafe struct _Bools {
            internal fixed byte Data[TaskConstants.MaxTasks / 4];

            public readonly Span<byte> AsSpan() {
                fixed (byte* ptr = Data) {
                    return new Span<byte>(ptr, TaskConstants.MaxTasks / 4);
                }
            }
        }

        internal unsafe struct _AdjacencyMatrix {
            public fixed byte Data[TaskConstants.MaxTasks * TaskConstants.MaxTasks];

            public readonly Span<byte> AsSpan() {
                fixed (byte* ptr = Data) {
                    return new Span<byte>(ptr, TaskConstants.MaxTasks * TaskConstants.MaxTasks);
                }
            }
        }

        // TODO: Fix the size because it supports up to 256 tasks in parallel, that likely won't happen.
        internal unsafe struct _TaskGroups {
            public fixed byte Data[TaskConstants.MaxTasks * 4];

            public readonly Span<Slice> AsSpan() {
                fixed (byte* ptr = Data) {
                    return new Span<Slice>(ptr, TaskConstants.MaxTasks);
                }
            }

            public readonly ReadOnlySpan<Slice> AsSpan(int count) {
                fixed (byte* ptr = Data) {
                    return new ReadOnlySpan<Slice>(ptr, count);
                }
            }
        }

        internal ReadOnlySpan<Slice> Groups => TaskGroups.AsSpan(GroupCount);

        // Stores an ordered list of TaskHandles
        internal DynamicArray<INode<ushort>> Nodes;
        internal DynamicArray<(INode<ushort> node, UnmanagedRef<TaskMetadata> metadata)> Sorted;
        internal DynamicArray<TaskMetadata> Metadata; // TODO: Sort the metadata also?
        internal _Bools Bytes;
        internal _Edges Edges;
        internal _AdjacencyMatrix AdjacencyMatrix;
        internal _TaskGroups TaskGroups;
        internal ushort GroupCount;

        internal WorkerBuffer WorkerBuffer;
        internal DynamicArray<TaskWorker> WorkerRefs;
        internal DynamicArray<(WorkerHandle workerHandle, UnmanagedRef<TaskMetadata> metadata)> Handles;

        // TODO: Write an allocation strategy
        public TaskGraph(int capacity) {
            Nodes = new DynamicArray<INode<ushort>>(capacity);
            Sorted = new DynamicArray<(INode<ushort>, UnmanagedRef<TaskMetadata>)>(capacity);
            Metadata = new DynamicArray<TaskMetadata>(capacity);

            for (var i = 0; i < capacity; i++) {
                Metadata.Add(new TaskMetadata());
            }
            Metadata.Clear();

            WorkerBuffer = new WorkerBuffer();
            WorkerRefs = new DynamicArray<TaskWorker>(TaskConstants.MaxTasks);
            Handles = new DynamicArray<(WorkerHandle, UnmanagedRef<TaskMetadata>)>(TaskConstants.MaxTasks);
        }

        public void Reset() {
            for (var i = 0; i < Nodes.Count; i++) {
                var node = Nodes[i];
                node.Dispose();
            }

            // Reset all metadata
            for (var i = 0; i < Metadata.Collection.Length; i++) {
                ref var metadata = ref Metadata.Collection[i];
                metadata.Reset();
            }

            Metadata.Clear();
            Nodes.Clear();
            Sorted.Clear();

            Bytes = default;
            Edges = default;
            AdjacencyMatrix = default;
            TaskGroups = default;
            GroupCount = 0;

            WorkerRefs.Clear();
            Handles.Clear();
        }

        public void Track(INode<ushort> trackedTask, TaskWorkload workload) {
            var span = Bytes.AsSpan();
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
            var adjacencyMatrix = AdjacencyMatrix.AsSpan();

            var taskCount = Nodes.Count;

            for (var i = 0; i < taskCount; i++) {
                var task = Nodes[i];

                foreach (var parentID in task.GetDependencies()) {
                    var parentIdx = Nodes.Find(
                        element => element.GlobalID == parentID);
                    var childIdx = i;

                    if (parentIdx > -1) {
                        adjacencyMatrix[(parentIdx * TaskConstants.MaxTasks) + childIdx] = 1;
                        inDegree[childIdx]++;
                    }
                }
            }

            PrintAdjacencyMatrix(adjacencyMatrix, taskCount);
            var totalTaskCountSq = taskCount * taskCount;

            Span<ushort> _queue = stackalloc ushort[TaskConstants.MaxTasks];
            var queue = new NoAllocQueue<ushort>(_queue);
            for (ushort i = 0; i < taskCount; i++) {
                if (inDegree[i] == 0) {
                    _ = queue.TryEnqueue(i);
                }
            }

            var taskGroups = TaskGroups.AsSpan();
            ushort batchIndex = 0;
            ushort offset = 0;
            while (queue.Count > 0) {
                var batchSize = (ushort)queue.Count;
                taskGroups[batchIndex] = new Slice {
                    Count = batchSize,
                    Start = offset
                };

                for (var i = 0; i < batchSize; i++) {
                    var taskIdx = queue.Dequeue();
                    Sorted.Add((Nodes[taskIdx],
                        new UnmanagedRef<TaskMetadata>(ref Metadata.ElementAt(taskIdx))));

                    for (ushort x = 0; x < taskCount; x++) {
                        if (adjacencyMatrix[(taskIdx * TaskConstants.MaxTasks) + x] == 1) {
                            inDegree[x]--;

                            if (inDegree[x] == 0) {
                                offset++;
                                _ = queue.TryEnqueue(x);
                            }
                        }
                    }
                }

                batchIndex++;
            }
            GroupCount = batchIndex;

            if (Sorted.Count != taskCount) {
                throw new InvalidOperationException("Cyclic dependencies occurred, aborting!");
            }
        }

        public void Process() {
            var taskGroups = TaskGroups.AsSpan(GroupCount);
            for (var i = 0; i < GroupCount; i++) {
                var slice = taskGroups[i];

                for (var x = 0; x < slice.Count; x++) {
                    var offset = x + slice.Start;
                    var element = Sorted[offset];
                    var metadata = element.metadata.Ref;
                    var task = element.node.Task;

                    switch (metadata.Workload.Type) {
                        case WorkloadType.Fake:
                            break;
                        case WorkloadType.SingleThreadNoLoop: {
                                void action() {
                                    task.Execute(-1);
                                }
                                var (handle, worker) = WorkerBuffer.Rent();
                                worker.Start(action, element.metadata);
                                WorkerRefs.Add(worker);
                                Handles.Add((handle, element.metadata));
                                break;
                            }
                        case WorkloadType.SingleThreadLoop: {
                                void action() {
                                    for (var i = 0; i < metadata.Workload.Length; i++) {
                                        task.Execute(i);
                                    }
                                }
                                var (handle, worker) = WorkerBuffer.Rent();
                                worker.Start(action, element.metadata);
                                WorkerRefs.Add(worker);
                                Handles.Add((handle, element.metadata));
                                break;
                            }
                        case WorkloadType.MultiThreadLoop: {
                                for (var t = 0; t < metadata.Workload.ThreadCount; t++) {
                                    var start = t * metadata.Workload.BatchSize;
                                    var diff = metadata.Workload.Total - start;
                                    var length = diff > metadata.Workload.BatchSize ?
                                        metadata.Workload.BatchSize : diff;

                                    void action() {
                                        for (var m = 0; m < length; m++) {
                                            var offset = m + start;
                                            task.Execute(offset);
                                        }
                                    }

                                    var (handle, worker) = WorkerBuffer.Rent();
                                    // TODO: Because we share the same state for all threads with the same metadata, and we create
                                    // another thread that does not start until later, the previous thread will set the Metadata to Completed.
                                    // This causes a queued thread for the same task to be dead.
                                    worker.Start(action, element.metadata, t);
                                    WorkerRefs.Add(worker);
                                    Handles.Add((handle, element.metadata));
                                }
                                break;
                            }
                        default:
                            throw new InvalidOperationException();
                    }
                }

                var workers = WorkerRefs.AsReadOnlySpan();
                TaskWorker.WaitAll(workers);
                for (var x = 0; x < WorkerRefs.Count; x++) {
                    var handle = Handles[x];
                    var worker = WorkerRefs[x];

                    var metadata = handle.metadata.Ref;
                    if (metadata.State == TaskState.Faulted) {
                        // TODO: Add a log handler
                        return;
                    }

                    // Return the worker
                    WorkerBuffer.Return((handle.workerHandle, worker));
                }
                WorkerRefs.Clear();
                Handles.Clear();
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
