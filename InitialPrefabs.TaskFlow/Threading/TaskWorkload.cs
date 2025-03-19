using InitialPrefabs.TaskFlow.Utils;
using System;
using System.Runtime.InteropServices;

namespace InitialPrefabs.TaskFlow.Threading {

    public enum WorkloadType : byte {
        Fake,
        SingleThreadNoLoop,
        SingleThreadLoop,
        MultiThreadLoop
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TaskWorkload : IEquatable<TaskWorkload> {
        [FieldOffset(0)]
        public WorkloadType Type;
        [FieldOffset(1)]
        public int Length;
        [FieldOffset(1)]
        public int Total;
        [FieldOffset(5)]
        public int BatchSize;

        public readonly int ThreadCount => Type switch {
            WorkloadType.Fake => 0,
            WorkloadType.SingleThreadNoLoop => 1,
            WorkloadType.SingleThreadLoop => 1,
            WorkloadType.MultiThreadLoop => MathUtils.CeilToIntDivision(Total, BatchSize),
            _ => throw new InvalidOperationException("Pick a valid TaskWorkload")
        };

        public static TaskWorkload SingleUnit() {
            return new TaskWorkload {
                Type = WorkloadType.SingleThreadNoLoop
            };
        }

        public static TaskWorkload LoopedSingleUnit(int length) {
            return new TaskWorkload {
                Type = WorkloadType.SingleThreadLoop,
                Length = length
            };
        }

        public static TaskWorkload MultiUnit(int total, int batchSize) {
            return new TaskWorkload {
                Type = WorkloadType.MultiThreadLoop,
                Total = total,
                BatchSize = batchSize
            };
        }

        public bool Equals(TaskWorkload other) {
            return other.Total == Total && BatchSize == other.BatchSize;
        }

        public override string ToString() {
            return Type switch {
                WorkloadType.Fake => "Fake",
                WorkloadType.SingleThreadNoLoop => "Single Thread With No Loops",
                WorkloadType.SingleThreadLoop => $"Single Thread With Loop Count: {Length}",
                WorkloadType.MultiThreadLoop => $"Multiple Threads: {ThreadCount} with Total: {Total}, BatchSize: {BatchSize}",
            };
        }
    }
}
