using InitialPrefabs.TaskFlow.Collections;
using InitialPrefabs.TaskFlow.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    public readonly struct WorkerHandle : IEquatable<WorkerHandle> {
        internal readonly byte Id;

        public WorkerHandle(byte id) {
            Id = id;
        }

        public readonly bool Equals(WorkerHandle other) {
            return other.Id == Id;
        }

        public static implicit operator byte(WorkerHandle value) {
            return value.Id;
        }
    }

    // The idea is that a UnitTask represents a single allocated
    // worker on a thread.
    public sealed class TaskWorker : IDisposable {

        internal sealed class WorkerContext {
            public UnmanagedRef<TaskMetadata> TaskMetadata { get; private set; }
            private int Length;
            private int Offset;
            private ITaskUnitRef task;

            public readonly Action ExecutionHandler;
            public readonly Action CompletionHandler;
            public int ThreadIndex { get; private set; }

            public override string ToString() {
                return task != null ? task.GetType().ToString() : "Empty";
            }

            public WorkerContext() {
                Length = 0;
                Offset = 0;
                ThreadIndex = -1;
                task = null;

                ExecutionHandler = () => {
                    for (var i = 0; i < Length; i++) {
                        task.Handler(i + Offset);
                    }
                };
            }

            public WorkerContext(Action completionHandler) : this() {
                CompletionHandler = completionHandler;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Bind(int offset, int length, ITaskUnitRef task, UnmanagedRef<TaskMetadata> taskMetadata, int threadIdx = -1) {
                Offset = offset;
                Length = length;
                this.task = task;
                TaskMetadata = taskMetadata;
                ThreadIndex = threadIdx;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Unbind() {
                Offset = 0;
                Length = 0;
                task = null;
                TaskMetadata = default;
            }

            public bool IsValid => Length > 0 && task != null && TaskMetadata.IsValid;
        }

        internal static readonly WaitCallback WorkItemHandler;

        static TaskWorker() {
            WorkItemHandler = Execute;
        }

        private static void Execute(object state) {
            var ctx = (WorkerContext)state;
#if DEBUG
            if (!ctx.IsValid) {
                throw new InvalidOperationException("Failed to execute the worker thread!");
            }
#endif

            ref var m = ref ctx.TaskMetadata.Ref;
            Console.WriteLine($"Thead Idx: {ctx.ThreadIndex}");
            try {
                ctx.ExecutionHandler.Invoke();
            } catch (Exception err) {
                LogUtils.Emit(err);
                m.State = TaskState.Faulted;
                m.Token.Cancel();
            } finally {
                ctx.CompletionHandler();
                if (m.State != TaskState.Faulted) {
                    m.State = TaskState.Completed;

                    // For multiple threads and loops, we need to put the completion flag.
                    if (ctx.ThreadIndex != -1) {
                        _ = Interlocked.Exchange(
                            ref ctx.TaskMetadata.Ref.CompletionFlags,
                            ctx.TaskMetadata.Ref.CompletionFlags | 1 << ctx.ThreadIndex);
                    }
                }
            }
        }

        internal readonly ManualResetEvent WaitHandle;
        internal readonly WorkerContext Context;

        public TaskWorker() {
            WaitHandle = new ManualResetEvent(false);
            Context = new WorkerContext(Complete);
        }

        public void Bind(ITaskUnitRef task, UnmanagedRef<TaskMetadata> taskMetadata, int threadIdx = -1) {
            Context.Bind(0, 1, task, taskMetadata, threadIdx);
        }

        public void Bind(int offset, int length, ITaskUnitRef task, UnmanagedRef<TaskMetadata> taskMetadata, int threadIdx = -1) {
            Context.Bind(offset, length, task, taskMetadata, threadIdx);
        }

        public void Start() {
            ref var m = ref Context.TaskMetadata.Ref;
            if (m.State != TaskState.NotStarted && m.Workload.Type != WorkloadType.MultiThreadLoop) {
                throw new InvalidOperationException(
                    "Cannot reuse the same RewindableUnitTask because the " +
                    "associated metadata indicates a thread is inflight.");
            }
            _ = ThreadPool.UnsafeQueueUserWorkItem(WorkItemHandler, Context);
        }

        public void Wait() {
            _ = WaitHandle.WaitOne();
        }

        public void Complete() {
            _ = WaitHandle.Set();
        }

        public bool Reset() {
            Context.Unbind();
            return WaitHandle.Reset();
        }

        public void Dispose() {
            WaitHandle.Dispose();
        }

        public static void StartAll(ReadOnlySpan<TaskWorker> tasks) {
            for (var i = 0; i < tasks.Length; i++) {
                var task = tasks[i];
                task.Start();
            }
        }

        public static void WaitAll(Span<TaskWorker> tasks) {
            for (var i = 0; i < tasks.Length; i++) {
                var task = tasks[i];
                task.Wait();
            }
        }

        public static void WaitAll(ReadOnlySpan<TaskWorker> tasks) {
            for (var i = 0; i < tasks.Length; i++) {
                var task = tasks[i];
                task.Wait();
            }
        }
    }
}

