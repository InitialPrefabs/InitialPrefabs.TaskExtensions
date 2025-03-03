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

    public struct Workload {
        public byte ThreadCount;
        public uint WorkDonePerThread;

        public static Workload SingleUnit(uint length) {
            return new Workload {
                ThreadCount = 1,
                WorkDonePerThread = length
            };
        }

        public static Workload MultiUnit(byte threadCount, uint length) {
            return new Workload {
                ThreadCount = threadCount,
                WorkDonePerThread = length
            };
        }
    }

    public class TaskMetadata<T0> where T0: struct, ITaskFor {
        public T0 Task;
        public TaskState State;
        public Workload Workload;

        public TaskMetadata() {
            Reset();
        }

        public void Reset() {
            Task = new T0();
            State = TaskState.NotStarted;
            Workload = new Workload();
        }
    }

    public readonly struct LocalHandle<T0> where T0 : struct, ITaskFor {
        public static readonly Type PrimaryType = typeof(T0);

        private readonly ushort index;

        public LocalHandle(ushort index) {
            this.index = index;
        }

        public readonly Type GetPrimaryType() {
            return PrimaryType;
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

        internal static readonly DynamicArray<TaskMetadata<T0>> Tasks;
        internal static readonly DynamicArray<ushort> FreeIndices;

        public static int Remaining => FreeIndices.Count;
        public static int Capacity => Tasks.Capacity;

        static TaskUnitPool() {
            var capacity = 5;
            Tasks = new DynamicArray<TaskMetadata<T0>>(capacity);
            FreeIndices = new DynamicArray<ushort>(capacity);

            for (ushort i = 0; i < 5; i++) {
                FreeIndices.Add(i);
                Tasks.Add(new TaskMetadata<T0>());
            }
        }

        /// <summary>
        /// Returns an allocated struct implementing <see cref="ITaskFor"/> to be copied into,
        /// </summary>
        /// <returns>A <see cref="LocalHandle{T0}"/> storing the index and Task container.</returns>
        public static LocalHandle<T0> Rent() {
            if (FreeIndices.IsEmpty) {
                var index = (ushort)Tasks.Count;
                Tasks.Add(new TaskMetadata<T0>());
                return new LocalHandle<T0>(index);
            } else {
                var lastIndex = FreeIndices.Count - 1;
                var freeIndex = FreeIndices[lastIndex];
                FreeIndices.RemoveAtSwapback(lastIndex);
                var task = Tasks[freeIndex];
                return new LocalHandle<T0>(freeIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(LocalHandle<T0> handle) {
            FreeIndices.Add(handle);
        }

        public static ref T0 TaskElementAt(LocalHandle<T0> handle) {
            return ref Tasks.Collection[handle].Task;
        }

        public static ref TaskMetadata<T0> ElementAt(LocalHandle<T0> handle) {
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

