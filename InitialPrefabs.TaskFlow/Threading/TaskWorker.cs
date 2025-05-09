using InitialPrefabs.TaskFlow.Collections;
using InitialPrefabs.TaskFlow.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
            private ITaskFor task;

            public readonly Action ExecutionHandler;
            public readonly Action CompletionHandler;
            public int ThreadIndex { get; private set; }

            public override string ToString() {
                return task != null ? task.GetType().ToString() : "Empty";
            }

            public WorkerContext() {
                Length = 0;
                Offset = 0;
                task = null;

                ExecutionHandler = () => {
                    for (var i = 0; i < Length; i++) {
                        task.Execute(i + Offset);
                    }
                };
            }

            public WorkerContext(Action completionHandler) : this() {
                CompletionHandler = completionHandler;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Bind(int offset, int length, ITaskFor task, UnmanagedRef<TaskMetadata> taskMetadata, int threadIdx = -1) {
                Offset = offset;
                Length = length;
                this.task = task;
                TaskMetadata = taskMetadata;
                ThreadIndex = -1;
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

        private static readonly WaitCallback WorkItemHandler;

        static TaskWorker() {
            WorkItemHandler = Execute;
        }

        private static void Execute(object state) {
            var ctx = (WorkerContext)state;
            if (!ctx.IsValid) {
                Console.WriteLine("Screwed up");
                throw new InvalidOperationException("Failed to execute the worker thread!");
            }

            ref var m = ref ctx.TaskMetadata.Ref;
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
                            1 << ctx.ThreadIndex);
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

        public void Bind(ITaskFor task, UnmanagedRef<TaskMetadata> taskMetadata, int threadIdx = -1) {
            Context.Bind(0, 1, task, taskMetadata, threadIdx);
        }

        public void Bind(int offset, int length, ITaskFor task, UnmanagedRef<TaskMetadata> taskMetadata, int threadIdx = -1) {
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

