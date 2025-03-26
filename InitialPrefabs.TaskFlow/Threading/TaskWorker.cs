using InitialPrefabs.TaskFlow.Collections;
using InitialPrefabs.TaskFlow.Utils;
using System;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    internal readonly struct WorkerHandle {
        public readonly TaskWorker Worker => Workers[Id];

        private readonly byte Id;

        private readonly TaskWorker[] Workers;

        public WorkerHandle(byte id, TaskWorker[] workers) {
            Id = id;
            Workers = workers;
        }
    }

    // The idea is that a UnitTask represents a single allocated
    // worker on a thread.
    public sealed class TaskWorker : IDisposable {

        internal readonly ManualResetEvent WaitHandle;

        public TaskWorker() {
            WaitHandle = new ManualResetEvent(false);
        }

        public void Start(Action action, UnmanagedRef<TaskMetadata> metadata) {
            ref var m = ref metadata.Ref;

            if (m.State != TaskState.NotStarted) {
                throw new InvalidOperationException("Cannot reuse the same RewindableUnitTask because the " +
                    "associated metadata indicates a thread is inflight.");
            }

            // When we start a new Unit Task, we have to go through the try catch finally block
            // to safely execute each thread
            _ = ThreadPool.UnsafeQueueUserWorkItem(_ => {
                ref var m = ref metadata.Ref;
                try {
                    action.Invoke();
                } catch (Exception err) {
                    LogUtils.Emit(err);
                    m.State = TaskState.Faulted;
                    m.Token.Cancel();
                } finally {
                    if (m.State != TaskState.Faulted) {
                        m.State = TaskState.Completed;
                    }
                    Complete();
                }
            }, null);
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
    }
}

