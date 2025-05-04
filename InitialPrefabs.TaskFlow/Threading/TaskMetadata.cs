using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public struct TaskMetadata : IEquatable<TaskMetadata> {

        public TaskState State;
        public TaskWorkload Workload;
        public AtomicCancellationToken Token;
        public FixedUInt16Array32 ExceptionReferences;
        public int CompletionFlags;

        public static TaskMetadata Default() {
            return new TaskMetadata {
                Token = new AtomicCancellationToken(),
                State = TaskState.NotStarted,
                Workload = default,
                ExceptionReferences = default,
                CompletionFlags = default
            };
        }

        public bool Equals(TaskMetadata other) {
            return other.State == State &&
                other.Workload.Equals(Workload);
        }
    }

    public static class TaskMetadataExtensions {
        public static void Reset(this ref TaskMetadata metadata) {
            metadata.State = TaskState.NotStarted;
            metadata.Workload = new TaskWorkload();
            metadata.Token.Reset();
            metadata.ExceptionReferences.Clear();
        }
    }
}

