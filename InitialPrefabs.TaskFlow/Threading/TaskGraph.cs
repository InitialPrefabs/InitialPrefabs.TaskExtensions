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
        internal DynamicArray<NodeMetadata> NodeMetadata;
        internal DynamicArray<ITaskUnitRef> TaskReferences;

        // TODO: Change this to hold the index into NodeMetadata, TaskReferences, and TaskMetadata
        internal DynamicArray<(ITaskUnitRef task, NodeMetadata node, UnmanagedRef<TaskMetadata> metadata)> Sorted;
        internal DynamicArray<TaskMetadata> TaskMetadata; // TODO: Sort the metadata also?
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
            NodeMetadata = new DynamicArray<NodeMetadata>(capacity);
            TaskReferences = new DynamicArray<ITaskUnitRef>(capacity);

            // Nodes = new DynamicArray<INode<ushort>>(capacity);
            Sorted = new DynamicArray<(ITaskUnitRef, NodeMetadata, UnmanagedRef<TaskMetadata>)>(capacity);
            TaskMetadata = new DynamicArray<TaskMetadata>(capacity);

            for (var i = 0; i < capacity; i++) {
                TaskMetadata.Add(new TaskMetadata());
            }
            TaskMetadata.Clear();

            WorkerBuffer = new WorkerBuffer();
            WorkerRefs = new DynamicArray<TaskWorker>(TaskConstants.MaxTasks);
            Handles = new DynamicArray<(WorkerHandle, UnmanagedRef<TaskMetadata>)>(TaskConstants.MaxTasks);
        }

        public void Reset() {
            // Reset all metadata
            for (var i = 0; i < TaskMetadata.Collection.Length; i++) {
                ref var metadata = ref TaskMetadata.Collection[i];
                metadata.Reset();
            }

            NodeMetadata.Clear();
            TaskReferences.Clear();
            TaskMetadata.Clear();
            // Nodes.Clear();
            Sorted.Clear();

            Bytes = default;
            Edges = default;
            AdjacencyMatrix = default;
            TaskGroups = default;
            GroupCount = 0;

            WorkerRefs.Clear();
            Handles.Clear();
        }

        public void Track<T0>(T0 trackedTask, TaskWorkload workload) where T0 : struct, INode<ushort> {
            var span = Bytes.AsSpan();
            var bitArray = new NoAllocBitArray(span);
            var nodeMetadata = trackedTask.Metadata;
            if (!bitArray[nodeMetadata.GlobalID]) {
                TaskMetadata.Add(new TaskMetadata(workload));
                // When we track a task, the associated metadata must also be enabled
                bitArray[nodeMetadata.GlobalID] = true;
                NodeMetadata.Add(trackedTask.Metadata);
                TaskReferences.Add(trackedTask.TaskRef);
                // FIXME: Track the task and node metadata instead
                // Nodes.Add(trackedTask);
            }
        }

        internal bool IsDependent(int index) {
            for (var i = 0; i < NodeMetadata.Count; i++) {
                if (i == index) {
                    continue;
                }

                var node = NodeMetadata[i];
                foreach (var dependency in node.Parents) {
                    if (dependency == index) {
                        return true;
                    }
                }
            }
            return false;
        }

        internal void Sort() {
            var taskCount = NodeMetadata.Count;
            if (taskCount == 0) {
                return;
            }

            Edges.Reset();
            var inDegree = Edges.AsSpan();
            Span<byte> _internalBytes = stackalloc byte[NoAllocBitArray.CalculateSize(
                TaskConstants.MaxTasks)];
            var visited = new NoAllocBitArray(_internalBytes);
            var adjacencyMatrix = AdjacencyMatrix.AsSpan();

            for (var i = 0; i < taskCount; i++) {
                var task = TaskReferences[i];
                var node = NodeMetadata[i];

                foreach (var parentID in node.Parents) {
                    var parentIdx = NodeMetadata.Find(
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
                    // TODO: This honestly looks correct...?
                    Sorted.Add((
                        TaskReferences[taskIdx],
                        NodeMetadata[taskIdx],
                        new UnmanagedRef<TaskMetadata>(ref TaskMetadata.ElementAt(taskIdx))));

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
                    (var task, var node, var metadataPtr) =
                        Sorted[offset];
                    var metadata = metadataPtr.Ref;

                    switch (metadata.Workload.Type) {
                        case WorkloadType.Fake:
                            break;
                        case WorkloadType.SingleThreadNoLoop: {
                                var (workerHandle, worker) = WorkerBuffer.Rent();
                                worker.Bind(task, metadataPtr);
                                WorkerRefs.Add(worker);
                                Handles.Add((workerHandle, metadataPtr));
                                break;
                            }
                        case WorkloadType.SingleThreadLoop: {
                                var (handle, worker) = WorkerBuffer.Rent();
                                worker.Bind(0, metadata.Workload.Length, task, metadataPtr);
                                WorkerRefs.Add(worker);
                                Handles.Add((handle, metadataPtr));
                                break;
                            }
                        case WorkloadType.MultiThreadLoop: {
                                ref var workload = ref metadata.Workload;
                                for (var threadIdx = 0; threadIdx < workload.ThreadCount; threadIdx++) {
                                    var start = threadIdx * workload.BatchSize;
                                    var diff = workload.Total - start;
                                    var length = diff > workload.BatchSize ?
                                        workload.BatchSize : diff;
                                    var (handle, worker) = WorkerBuffer.Rent();
                                    worker.Bind(start, length, task, metadataPtr, threadIdx);
                                    WorkerRefs.Add(worker);
                                    Handles.Add((handle, metadataPtr));
                                }
                                break;
                            }
                        default:
                            throw new InvalidOperationException();
                    }
                }

                var workers = WorkerRefs.AsReadOnlySpan();
                TaskWorker.StartAll(workers);
                TaskWorker.WaitAll(workers);
                WorkerBuffer.ReturnAll();
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
