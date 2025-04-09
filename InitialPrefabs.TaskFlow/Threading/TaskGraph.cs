using InitialPrefabs.TaskFlow.Collections;
using InitialPrefabs.TaskFlow.Utils;
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

        // Stores an ordered list of TaskHandles
        internal DynamicArray<INode<ushort>> Nodes;
        internal DynamicArray<(INode<ushort> node, UnmanagedRef<TaskMetadata> metadata)> Sorted;
        internal DynamicArray<TaskMetadata> Metadata; // TODO: Sort the metadata also?
        internal MaxByteBools Bytes;
        internal _Edges Edges;
        internal _AdjacencyMatrix AdjacencyMatrix;

        // Task Queue section
        internal ConcurrentQueue<(INode<ushort> node, UnmanagedRef<TaskMetadata> metadata)> TaskQueue;
        internal int RunningTasks;

        internal WorkerBuffer WorkerBuffer;
        internal DynamicArray<TaskWorker> Workers;
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

            TaskQueue = new ConcurrentQueue<(INode<ushort> node, UnmanagedRef<TaskMetadata> metadata)>();
            WorkerBuffer = new WorkerBuffer();
            Workers = new DynamicArray<TaskWorker>(TaskConstants.MaxTasks);
            Handles = new DynamicArray<(WorkerHandle, UnmanagedRef<TaskMetadata>)>(TaskConstants.MaxTasks);
        }

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
                        unsafe {
                            Console.WriteLine($"Child: {childIdx}: {inDegree[childIdx]}, {Edges.Data[childIdx]}");
                        }
                    }
                }
            }

            PrintAdjacencyMatrix(adjacencyMatrix, taskCount);
            var totalTaskCountSq = taskCount * taskCount;
            var copy = AdjacencyMatrix;
            var slicedMatrix = copy.AsSpan();

            // Copy the bigger matrix into the smaller matrix
            for (var i = 0; i < totalTaskCountSq; i++) {
                slicedMatrix[i] = adjacencyMatrix[i];
            }

            Span<ushort> _queue = stackalloc ushort[TaskConstants.MaxTasks];
            var queue = new NoAllocQueue<ushort>(_queue);
            for (ushort i = 0; i < taskCount; i++) {
                if (inDegree[i] == 0) {
                    _ = queue.TryEnqueue(i);
                }
            }

            var _edgeCopy = Edges;
            var copyInDegree = _edgeCopy.AsSpan();

            while (queue.Count > 0) {
                var taskIdx = queue.Dequeue();
                Sorted.Add((Nodes[taskIdx],
                    new UnmanagedRef<TaskMetadata>(ref Metadata.ElementAt(taskIdx))));
                for (ushort x = 0; x < taskCount; x++) {
                    if (slicedMatrix[(taskIdx * TaskConstants.MaxTasks) + x] == 1) {
                        copyInDegree[x]--;

                        if (copyInDegree[x] == 0) {
                            _ = queue.TryEnqueue(x);
                        }
                    }
                }
            }

            for (var i = 0; i < inDegree.Length; i++) {
                Console.WriteLine($"{i}: {inDegree[i]}");
            }

            foreach (var e in Sorted) {
                Console.WriteLine($"Task ID: {e.node.GlobalID}");
            }

            if (Sorted.Count != taskCount) {
                throw new InvalidOperationException("Cyclic dependencies occurred, aborting!");
            }
        }

        public int EnqueueTasks(int start) {
            if (start > 0) {
                start++;
            }
            var inDegree = Edges.AsSpan();
            var idx = -1;
            Console.WriteLine($"Starting from: {start}");
            for (var i = start; i < Sorted.Count; i++) {
                var element = Sorted[i];

                if (inDegree[element.node.GlobalID] == 0) {
                    _ = Interlocked.Increment(ref RunningTasks);
                    Console.WriteLine($"Enqueued: {element.node.GlobalID}");
                    TaskQueue.Enqueue(element);
                    idx = i;
                }
            }
            return idx;
        }

        public void Process() {
            var inDegree = Edges.AsSpan();

            // Main thread implementation
            var idx = EnqueueTasks(0);

            var taskCount = Sorted.Count;
            Span<ushort> _list = stackalloc ushort[taskCount];
            var list = new NoAllocList<ushort>(_list);

            while (RunningTasks > 0) {
                // We dequeued the tasks and tracked them, but we need to somehow wait
                if (TaskQueue.TryDequeue(out var element)) {


                    // Find the index in the sorted.
                    var taskIdx =
                        Sorted.Find(s => s.node == element.node);

                    list.Add((ushort)taskIdx);

                    Console.WriteLine(element.metadata.Ref.State);
                    // We have to dequeue the task. Figure out how many Workers we need to spawn
                    var workload = element.metadata.Ref.Workload;
                    var task = element.node.Task;

                    switch (workload.Type) {
                        case WorkloadType.Fake:
                            break;
                        case WorkloadType.SingleThreadNoLoop: {
                                // Create an action
                                void action() {
                                    task.Execute(-1);
                                }

                                // Launch a worker, and we have to store the worker handle and the metadata.
                                var rented = WorkerBuffer.Rent();
                                rented.worker.Start(action, element.metadata);
                                Workers.Add(rented.worker);
                                Handles.Add((rented.handle, element.metadata));
                                break;
                            }
                        case WorkloadType.SingleThreadLoop: {
                                void action() {
                                    var metadata = element.metadata;
                                    for (var i = 0; i < workload.Length && !metadata.Ref.Token.IsCancellationRequested; i++) {
                                        task.Execute(i);
                                    }
                                }
                                break;
                            }
                        case WorkloadType.MultiThreadLoop: {
                                for (var x = 0; x < workload.ThreadCount; x++) {
                                    // Determine the slice
                                    var startOffset = x * workload.BatchSize;
                                    var diff = workload.Total - startOffset;
                                    var length = diff > workload.BatchSize ? workload.BatchSize : diff;

                                    void action() {
                                        var metadata = element.metadata;
                                        for (var i = 0; i < length && !metadata.Ref.Token.IsCancellationRequested; i++) {
                                            var idx = startOffset + i;
                                            task.Execute(idx);
                                        }
                                    }

                                    // Now create a unit task
                                }
                                break;
                            }
                    }
                }

                // If we drained the first tasks from the task queue, we need to wait.
                if (TaskQueue.IsEmpty) {
                    var workers = Workers.AsReadOnlySpan();

                    TaskWorker.WaitAll(workers);
                    // Now we have to decrement and enqueue more tasks.
                    for (var i = 0; i < Handles.Count; i++) {
                        var handle = Handles[i];
                        var worker = Workers[i];

                        var metadata = handle.metadata.Ref;
                        if (metadata.State == TaskState.Faulted) {
                            // TODO: Add a log handler
                            return;
                        }

                        WorkerBuffer.Return((handle.workerHandle, worker));
                    }

                    RunningTasks = Interlocked.Exchange(
                        ref RunningTasks, RunningTasks - workers.Length);

                    // Clear the workers
                    Workers.Clear();

                    foreach (var taskIdx in list) {
                        var slicedMatrix = AdjacencyMatrix.AsSpan();
                        for (ushort x = 0; x < taskCount; x++) {
                            if (slicedMatrix[(taskIdx * TaskConstants.MaxTasks) + x] == 1) {
                                Console.WriteLine("Dec");
                                inDegree[x]--;
                            }

                            Console.WriteLine($"{x}: {inDegree[x]}");
                        }

                        PrintAdjacencyMatrix(slicedMatrix, taskCount);
                    }

                    list.Clear();

                    // TODO: Queue the next tasks, maybe store the index
                    idx = EnqueueTasks(idx);
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
