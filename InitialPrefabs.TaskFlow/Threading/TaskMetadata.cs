using System;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    public class AtomicCancellationToken {
        private int state;

        public bool IsCancellationRequested => state > 0;

        public void Cancel() {
            _ = Interlocked.Exchange(ref state, 1);
        }

        public void Reset() {
            _ = Interlocked.Exchange(ref state, 0);
        }
    }

    public class TaskMetadata : IEquatable<TaskMetadata> {
        public TaskState State;
        public TaskWorkload Workload;

        public TaskMetadata() {
            Reset();
        }

        public bool Equals(TaskMetadata other) {
            return other.State == State &&
                other.Workload.Equals(Workload);
        }

        public void Reset() {
            State = TaskState.NotStarted;
            Workload = new TaskWorkload();
        }
    }
}

