using System;

namespace InitialPrefabs.TaskFlow.Threading {
    public class TaskMetadata : IEquatable<TaskMetadata> {
        public TaskState State { get; private set; }
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


