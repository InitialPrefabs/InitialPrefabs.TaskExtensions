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
            public UnmanagedRef<TaskMetadata> TaskMetadata;
            public Action OnRun;
            public Action OnComplete;
            public int ThreadIndex;
            public ushort ExecutionCtxHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Bind(
                UnmanagedRef<TaskMetadata> metadata,
                Action taskAction,
                Action onComplete,
                int threadIdx,
                ushort ctxHandle) {

                TaskMetadata = metadata;
                OnRun = taskAction;
                OnComplete = onComplete;
                ThreadIndex = threadIdx;
                ExecutionCtxHandle = ctxHandle;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Unbind() {
                OnRun = null;
                TaskMetadata = default;
                OnComplete = null;
                ThreadIndex = -1;
                ExecutionCtxHandle = 0;
            }
        }

        private static readonly WaitCallback ExecuteHandler;

        static TaskWorker() {
            ExecuteHandler = Execute;
        }

        private static void Execute(object state) {
            var ctx = (WorkerContext)state;
            ref var m = ref ctx.TaskMetadata.Ref;
            try {
                ctx.OnRun.Invoke();
            } catch (Exception err) {
                LogUtils.Emit(err);
                m.State = TaskState.Faulted;
                m.Token.Cancel();
            } finally {
                ctx.OnComplete();
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
        internal readonly Action CompleteHandler;

        public TaskWorker() {
            WaitHandle = new ManualResetEvent(false);
            Context = new WorkerContext();
            CompleteHandler = Complete;
        }

        public void Bind(
            Action action,
            UnmanagedRef<TaskMetadata> metadata,
            int threadIndex,
            ushort ctxHandle) {
            Context.Bind(
                metadata,
                action,
                CompleteHandler,
                threadIndex,
                ctxHandle);
        }

        public void Start() {
            ref var m = ref Context.TaskMetadata.Ref;
            if (m.State != TaskState.NotStarted && m.Workload.Type != WorkloadType.MultiThreadLoop) {
                throw new InvalidOperationException(
                    "Cannot reuse the same RewindableUnitTask because the " +
                    "associated metadata indicates a thread is inflight.");
            }
            _ = ThreadPool.UnsafeQueueUserWorkItem(ExecuteHandler, Context);
        }

        public void Wait() {
            _ = WaitHandle.WaitOne();
        }

        public void Complete() {
            _ = WaitHandle.Set();
            ExecutionContextBuffer.Return(Context.ExecutionCtxHandle);
            Context.Unbind();
        }

        public bool Reset() {
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

