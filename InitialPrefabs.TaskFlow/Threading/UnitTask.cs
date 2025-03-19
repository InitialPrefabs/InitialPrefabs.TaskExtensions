using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    // The idea is that a UnitTask represents a single allocated
    // worker on a thread.
    public struct UnitTask {

        public void Start([NotNull] Action action, UnmanagedRef<TaskMetadata> metadata) {
            // When we start a new Unit Task, we have to go through the try catch finally block
            // to safely execute each thread
            _ = ThreadPool.UnsafeQueueUserWorkItem(_ => {
                try {
                    action.Invoke();
                } catch (Exception err) {
                    ref var m = ref metadata.Ref;
                    m.State = TaskState.Faulted;
                    m.Token.Cancel();
                } finally {
                    ref var m = ref metadata.Ref;
                }
            }, null);
        }
    }
}

