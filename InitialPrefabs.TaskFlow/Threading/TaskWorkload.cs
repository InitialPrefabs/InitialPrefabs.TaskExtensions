using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public struct TaskWorkload : IEquatable<TaskWorkload> {
        public byte Total;
        public uint BatchSize;

        public static TaskWorkload SingleUnit(uint length) {
            return new TaskWorkload {
                Total = 1,
                BatchSize = length
            };
        }

        public static TaskWorkload MultiUnit(byte threadCount, uint length) {
            return new TaskWorkload {
                Total = threadCount,
                BatchSize = length
            };
        }

        public bool Equals(TaskWorkload other) {
            return other.Total == Total && BatchSize == other.BatchSize;
        }
    }
}


