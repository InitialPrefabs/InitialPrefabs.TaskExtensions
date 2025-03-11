using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow.Collections {

    /// <summary>
    /// State of the Task.
    /// </summary>
    public enum TaskState {
        NotStarted,
        InProgress,
        Success,
        Faulted
    }

    public struct Workload : IEquatable<Workload> {
        public byte Total;
        public uint BatchSize;

        public static Workload SingleUnit(uint length) {
            return new Workload {
                Total = 1,
                BatchSize = length
            };
        }

        public static Workload MultiUnit(byte threadCount, uint length) {
            return new Workload {
                Total = threadCount,
                BatchSize = length
            };
        }

        public bool Equals(Workload other) {
            return other.Total == Total && BatchSize == other.BatchSize;
        }
    }

    public class TaskMetadata : IEquatable<TaskMetadata> {
        public TaskState State { get; private set; }
        public Workload Workload;

        public TaskMetadata() {
            Reset();
        }

        public bool Equals(TaskMetadata other) {
            return other.State == State &&
                other.Workload.Equals(Workload);
        }

        public void Reset() {
            State = TaskState.NotStarted;
            Workload = new Workload();
        }
    }

    public readonly struct LocalHandle<T0> : IEquatable<LocalHandle<T0>> where T0 : struct, ITaskFor {
        private readonly ushort index;

        public LocalHandle(ushort index) {
            this.index = index;
        }

        public bool Equals(LocalHandle<T0> other) {
            return other.index == index;
        }

        public static implicit operator ushort(LocalHandle<T0> value) {
            return value.index;
        }

        public static implicit operator LocalHandle<T0>(ushort value) {
            return new LocalHandle<T0>(value);
        }
    }

    /// <summary>
    /// Stores structs implement <see cref="ITaskFor"/> to avoid boxing on runtime.
    /// When <see cref="Rent"/> is called, a <see cref="LocalHandle{T0}"/> is returned providing
    /// the index of the <see cref="ITaskFor"/> struct.
    /// </summary>
    public static class TaskUnitPool<T0> where T0 : struct, ITaskFor {

        internal static readonly DynamicArray<T0> Tasks;
        internal static readonly DynamicArray<ushort> FreeIndices;

        public static int Remaining => FreeIndices.Count;
        public static int Capacity => Tasks.Capacity;

        static TaskUnitPool() {
            var capacity = 5;
            Tasks = new DynamicArray<T0>(capacity);
            FreeIndices = new DynamicArray<ushort>(capacity);

            for (ushort i = 0; i < 5; i++) {
                FreeIndices.Add(i);
                Tasks.Add(new T0());
            }
        }

        /// <summary>
        /// Returns an allocated struct implementing <see cref="ITaskFor"/> to be copied into,
        /// </summary>
        /// <param name="value">The task to perform a copy on into the TaskUnitPool.</param>
        /// <returns>A <see cref="LocalHandle{T0}"/> storing the index and Task container.</returns>
        public static LocalHandle<T0> Rent(T0 value) {
            if (FreeIndices.IsEmpty) {
                var index = (ushort)Tasks.Count;
                Tasks.Add(value);
                return new LocalHandle<T0>(index);
            } else {
                var lastIndex = FreeIndices.Count - 1;
                var freeIndex = FreeIndices[lastIndex];
                FreeIndices.RemoveAtSwapback(lastIndex);
                Tasks[freeIndex] = value;
                return new LocalHandle<T0>(freeIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(LocalHandle<T0> handle) {
            FreeIndices.Add(handle);
        }

        public static ref T0 TaskElementAt(LocalHandle<T0> handle) {
            return ref Tasks.Collection[handle];
        }

        public static void Reset() {
            var capacity = Tasks.Capacity;
            Tasks.ForceResize(capacity);
            FreeIndices.ForceResize(capacity);
            FreeIndices.Clear();

            for (ushort i = 0; i < capacity; i++) {
                FreeIndices.Add(i);
            }
        }
    }
}

