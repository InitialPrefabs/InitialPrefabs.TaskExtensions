using System;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    public class TaskMetadata : IEquatable<TaskMetadata> {
        public TaskState State { get; private set; }
        public TaskWorkload Workload;

        private ManualResetEvent waitHandle;
        private Exception err;

        public TaskMetadata() {
            waitHandle = new ManualResetEvent(true);
            Reset();
        }

        public void Run<T>(T task) where T : struct, ITaskFor {
        }

        public bool Equals(TaskMetadata other) {
            return other.State == State &&
                other.Workload.Equals(Workload);
        }

        public void Reset() {
            State = TaskState.NotStarted;
            Workload = new TaskWorkload();
            _ = waitHandle.Reset();
        }
    }
}


