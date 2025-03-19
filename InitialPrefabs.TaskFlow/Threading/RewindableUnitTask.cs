using InitialPrefabs.TaskFlow.Collections;
using InitialPrefabs.TaskFlow.Utils;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    // The idea is that a UnitTask represents a single allocated
    // worker on a thread.
    public class RewindableUnitTask : IDisposable {

        public readonly ManualResetEvent WaitHandle;

        public RewindableUnitTask() {
            WaitHandle = new ManualResetEvent(false);
        }

        public RewindableUnitTask Start([NotNull] Action action, UnmanagedRef<TaskMetadata> metadata) {
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

            return this;
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

        public static void WaitAll(Span<RewindableUnitTask> tasks) {
            foreach (var task in tasks) {
                task.Wait();
            }
        }
    }
}

