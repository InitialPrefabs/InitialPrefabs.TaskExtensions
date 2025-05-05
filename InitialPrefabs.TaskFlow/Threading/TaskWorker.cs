using InitialPrefabs.TaskFlow.Collections;
using InitialPrefabs.TaskFlow.Utils;
using System;
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

        private struct Payload {
            public UnmanagedRef<TaskMetadata> TaskMetadata;
            public Action TaskAction;
            public Action OnComplete;
            public int Index;
        };

        internal readonly ManualResetEvent WaitHandle;

        public TaskWorker() {
            WaitHandle = new ManualResetEvent(false);
        }

        public void Start(Action action, UnmanagedRef<TaskMetadata> metadata) {
            ref var m = ref metadata.Ref;

            if (m.State != TaskState.NotStarted) {
                throw new InvalidOperationException(
                    "Cannot reuse the same RewindableUnitTask because the " +
                    "associated metadata indicates a thread is inflight.");
            }

            // When we start a new Unit Task, we have to go through the try catch finally block
            // to safely execute each thread
            _ = ThreadPool.UnsafeQueueUserWorkItem(static state => {
                var payload = (Payload)state;
                ref var m = ref payload.TaskMetadata.Ref;
                try {
                    payload.TaskAction();
                } catch (Exception err) {
                    LogUtils.Emit(err);
                    m.State = TaskState.Faulted;
                    m.Token.Cancel();
                } finally {
                    payload.OnComplete();
                    if (m.State != TaskState.Faulted) {
                        m.State = TaskState.Completed;
                    }
                }
            }, new Payload {
                OnComplete = Complete,
                TaskAction = action,
                TaskMetadata = metadata
            });
        }

        public void Start(Action action, UnmanagedRef<TaskMetadata> metadata, int index) {
            ref var m = ref metadata.Ref;

            if (m.State != TaskState.NotStarted && m.Workload.Type != WorkloadType.MultiThreadLoop) {
                throw new InvalidOperationException(
                    "Cannot reuse the same RewindableUnitTask because the " +
                    "associated metadata indicates a thread is inflight.");
            }

            // When we start a new Unit Task, we have to go through the try catch finally block
            // to safely execute each thread
            _ = ThreadPool.UnsafeQueueUserWorkItem(static state => {
                var payload = (Payload)state;
                ref var m = ref payload.TaskMetadata.Ref;
                try {
                    payload.TaskAction.Invoke();
                } catch (Exception err) {
                    LogUtils.Emit(err);
                    m.State = TaskState.Faulted;
                    m.Token.Cancel();
                } finally {
                    payload.OnComplete();
                    _ = Interlocked.Exchange(ref m.CompletionFlags, 1 << payload.Index);
                }
            }, new Payload {
                Index = index,
                OnComplete = Complete,
                TaskAction = action,
                TaskMetadata = metadata
            });
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

