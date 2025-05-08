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

        // TODO: Turn this into a class and pool it them, because we need to avoid boxing and reuse
        // payloads each frame.
        internal sealed class State {
            public UnmanagedRef<TaskMetadata> TaskMetadata;
            public Action TaskAction;
            public Action OnComplete;
            public int ThreadIndex;
            public ushort TaskContextHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Bind(
                UnmanagedRef<TaskMetadata> metadata,
                Action taskAction,
                Action onComplete,
                int threadIdx,
                ushort ctxHandle) {

                TaskMetadata = metadata;
                TaskAction = taskAction;
                OnComplete = onComplete;
                ThreadIndex = threadIdx;
                TaskContextHandle = ctxHandle;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Unbind() {
                TaskAction = null;
                TaskMetadata = default;
                OnComplete = null;
                ThreadIndex = -1;
                TaskContextHandle = 0;
            }
        }

        internal readonly ManualResetEvent WaitHandle;
        internal readonly State Payload;

        public TaskWorker() {
            WaitHandle = new ManualResetEvent(false);
            Payload = new State();
        }

        private static void Execute(object state) {
            var payload = (State)state;
            ref var m = ref payload.TaskMetadata.Ref;
            try {
                payload.TaskAction.Invoke();
            } catch (Exception err) {
                LogUtils.Emit(err);
                m.State = TaskState.Faulted;
                m.Token.Cancel();
            } finally {
                payload.OnComplete();
                TaskWrapperBuffer.Return(payload.TaskContextHandle);
                if (m.State != TaskState.Faulted) {
                    m.State = TaskState.Completed;

                    // For multiple threads and loops, we need to put the completion flag.
                    if (payload.ThreadIndex != -1) {
                        _ = Interlocked.Exchange(
                            ref payload.TaskMetadata.Ref.CompletionFlags,
                            1 << payload.ThreadIndex);
                    }
                }
            }
        }

        public void Enqueue(
            Action action,
            UnmanagedRef<TaskMetadata> metadata,
            int threadIndex,
            ushort ctxHandle) {
            Payload.Bind(
                metadata,
                action,
                Complete,
                threadIndex,
                ctxHandle);
        }

        public void Start() {
            ref var m = ref Payload.TaskMetadata.Ref;
            if (m.State != TaskState.NotStarted && m.Workload.Type != WorkloadType.MultiThreadLoop) {
                throw new InvalidOperationException(
                    "Cannot reuse the same RewindableUnitTask because the " +
                    "associated metadata indicates a thread is inflight.");
            }
            _ = ThreadPool.UnsafeQueueUserWorkItem(Execute, Payload);
        }

        public void Wait() {
            _ = WaitHandle.WaitOne();
        }

        public void Complete() {
            _ = WaitHandle.Set();
        }

        public bool Reset() {
            return WaitHandle.Reset();
        }

        public void Dispose() {
            WaitHandle.Dispose();
        }

        public static void StartAll(ReadOnlySpan<TaskWorker> tasks) {
            foreach (var task in tasks) {
                task.Start();
            }
        }

        public static void WaitAll(Span<TaskWorker> tasks) {
            foreach (var task in tasks) {
                task.Wait();
            }
        }

        public static void WaitAll(ReadOnlySpan<TaskWorker> tasks) {
            foreach (var task in tasks) {
                task.Wait();
            }
        }
    }
}

