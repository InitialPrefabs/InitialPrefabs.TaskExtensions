using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskFlow.Threading {

    public static class TaskConstants {
        internal const int MaxTasks = 256;
    }

    public class TaskGraph {

        /// <summary>
        /// We won't know how many tasks we track, so we reserve the total # of tasks we support.
        /// This is effectively a giant bitflag to ensure we dont accidentally track the same
        /// tasks over and over again.
        /// </summary>
        internal unsafe struct _Bools {
            internal fixed byte Data[TaskConstants.MaxTasks / 4];

            public readonly Span<byte> AsSpan() {
                fixed (byte* ptr = Data) {
                    return new Span<byte>(ptr, TaskConstants.MaxTasks / 4);
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

        internal DynamicArray<ushort> Sorted;
        internal DynamicArray<TaskMetadata> TaskMetadata;
        internal _Bools TrackingFlags;
        internal _Bools CompletionFlags;
        internal _TaskGroups TaskGroups;
        internal ushort GroupCount;

        internal WorkerBuffer WorkerBuffer;
        internal DynamicArray<TaskWorker> WorkerRefs;
        internal DynamicArray<(WorkerHandle workerHandle, UnmanagedRef<TaskMetadata> metadata)> Handles;

        public TaskGraph(int taskCapacity) : this(taskCapacity, TaskConstants.MaxTasks) { }

        // TODO: Write an allocation strategy
        public TaskGraph(int taskCapacity, int workerCapacity) {
            NodeMetadata = new DynamicArray<NodeMetadata>(taskCapacity);
            TaskReferences = new DynamicArray<ITaskUnitRef>(taskCapacity);

            // Nodes = new DynamicArray<INode<ushort>>(capacity);
            Sorted = new DynamicArray<ushort>(taskCapacity);
            TaskMetadata = new DynamicArray<TaskMetadata>(taskCapacity);

            for (var i = 0; i < taskCapacity; i++) {
                TaskMetadata.Add(new TaskMetadata());
            }
            TaskMetadata.Clear();

            WorkerBuffer = new WorkerBuffer(workerCapacity);
            WorkerRefs = new DynamicArray<TaskWorker>(workerCapacity);
            Handles = new DynamicArray<(WorkerHandle, UnmanagedRef<TaskMetadata>)>(workerCapacity);
        }

        ~TaskGraph() {
            Reset();
            WorkerBuffer.Dispose();
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
            Sorted.Clear();

            TrackingFlags = default;
            CompletionFlags = default;
            TaskGroups = default;
            GroupCount = 0;

            WorkerRefs.Clear();
            Handles.Clear();
        }

        public void Track<T0>(T0 trackedTask, TaskWorkload workload) where T0 : struct, INode<ushort> {
            var trackingFlags = new NoAllocBitArray(TrackingFlags.AsSpan());
            var nodeMetadata = trackedTask.Metadata;
            if (!trackingFlags[nodeMetadata.GlobalID]) {
                TaskMetadata.Add(new TaskMetadata(workload));
                // When we track a task, the associated metadata must also be enabled
                trackingFlags[nodeMetadata.GlobalID] = true;
                NodeMetadata.Add(trackedTask.Metadata);
                TaskReferences.Add(trackedTask.TaskRef);
            }
        }

        internal void Sort() {
            Sorted.Clear(); // Clear all sorted.
            var taskCount = NodeMetadata.Count;
            if (taskCount == 0) {
                return;
            }

            Span<byte> inDegree = stackalloc byte[taskCount];
            Span<byte> _internalBytes = stackalloc byte[NoAllocBitArray.CalculateSize(taskCount)];
            var visited = new NoAllocBitArray(_internalBytes);
            Span<byte> adjacencyMatrix = stackalloc byte[taskCount * taskCount];

            for (var i = 0; i < taskCount; i++) {
                var task = TaskReferences[i];
                var node = NodeMetadata[i];

                foreach (var parentID in node.Parents) {
                    var parentIdx = NodeMetadata.Find(
                        element => element.GlobalID == parentID);
                    var childIdx = i;

                    if (parentIdx > -1) {
                        adjacencyMatrix[(parentIdx * taskCount) + childIdx] = 1;
                        inDegree[childIdx]++;
                    }
                }
            }

            PrintAdjacencyMatrix(adjacencyMatrix, taskCount);

            Span<ushort> _queue = stackalloc ushort[taskCount];
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
                    Sorted.Add(taskIdx);

                    for (ushort x = 0; x < taskCount; x++) {
                        if (adjacencyMatrix[(taskIdx * taskCount) + x] == 1) {
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
                throw new InvalidOperationException($"Cyclic dependencies occurred, aborting! Sorted Count: {Sorted.Count}, Total Task Count: {taskCount}");
            }
        }

        public async void ProcessAsync() {
            await Task.Factory.StartNew(Process);
        }

        public void Process() {
            var taskGroups = TaskGroups.AsSpan(GroupCount);
            var flags = new NoAllocBitArray(CompletionFlags.AsSpan());
            for (var i = 0; i < GroupCount; i++) {
                var slice = taskGroups[i];

                for (var x = 0; x < slice.Count; x++) {
                    var index = x + slice.Start;
                    // (var task, var node, var metadataPtr) =
                    var sortIdx = Sorted[index];
                    var task = TaskReferences[sortIdx];
                    var node = NodeMetadata[sortIdx];

                    if (flags[node.GlobalID]) {
                        continue;
                    }

                    var metadataPtr = new UnmanagedRef<TaskMetadata>(ref TaskMetadata.Collection[sortIdx]);

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

                var completionFlags= new NoAllocBitArray(CompletionFlags.AsSpan());
                // We have to mark which nodes have finished.
                for (var x = 0; x < slice.Count; x++) {
                    var index = x + slice.Start;
                    var sortIdx = Sorted[index];
                    var node = NodeMetadata[sortIdx];
                    completionFlags[node.GlobalID] = true;
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
