using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow.Threading {

    public interface ITaskUnitRef {
        public Action<int> Handler { get; }
    }

    public class TaskUnitRef<T0> : ITaskUnitRef where T0 : struct, ITaskFor {
        public T0 Task;

        public Action<int> Handler { get; }

        private TaskUnitRef() {
            Task = default;
            Handler = Execute;
        }

        public TaskUnitRef(T0 task) : this() {
            this.Task = task;
        }

        private void Execute(int index) {
            Task.Execute(index);
        }
    }

    /// <summary>
    /// Stores structs implement <see cref="ITaskFor"/> to avoid boxing on runtime.
    /// When <see cref="Rent"/> is called, a <see cref="LocalHandle{T0}"/> is returned providing
    /// the index of the <see cref="ITaskFor"/> struct.
    /// </summary>
    public static class TaskUnitPool<T0> where T0 : struct, ITaskFor {

        internal static readonly DynamicArray<TaskUnitRef<T0>> Tasks;
        internal static readonly DynamicArray<ushort> FreeHandles;
        internal static readonly DynamicArray<ushort> UsedHandles;

        public static int Remaining => FreeHandles.Count;
        public static int Capacity => Tasks.Capacity;

        static TaskUnitPool() {
            var capacity = 5;
            Tasks = new DynamicArray<TaskUnitRef<T0>>(capacity);
            FreeHandles = new DynamicArray<ushort>(capacity);
            UsedHandles = new DynamicArray<ushort>(capacity);

            for (ushort i = 0; i < 5; i++) {
                FreeHandles.Add(i);
                Tasks.Add(new TaskUnitRef<T0>(new T0()));
            }

            TaskGraphManager.OnReset += Reset;
        }

        /// <summary>
        /// Returns an allocated struct implementing <see cref="ITaskFor"/> to be copied into.
        /// </summary>
        /// <param name="value">The task to perform a copy on into the TaskUnitPool.</param>
        /// <returns>A <see cref="LocalHandle{T0}"/> storing the index and Task container.</returns>
        public static LocalHandle Rent(T0 value) {
            if (FreeHandles.IsEmpty) {
                var freeHandle = (ushort)Tasks.Count;
                UsedHandles.Add(freeHandle);
                Tasks.Add(new TaskUnitRef<T0>(value));
                return new LocalHandle(freeHandle);
            } else {
                var lastIndex = FreeHandles.Count - 1;
                var freeHandle = FreeHandles[lastIndex];
                UsedHandles.Add(freeHandle);
                FreeHandles.RemoveAtSwapback(lastIndex);
                var unitRef = Tasks[freeHandle];
                unitRef.Task = value;
                return new LocalHandle(freeHandle);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(LocalHandle handle) {
            FreeHandles.Add(handle);
        }

        public static ref TaskUnitRef<T0> ElementAt(LocalHandle handle) {
            return ref Tasks.Collection[handle];
        }

        public static void Reset() {
            for (var i = 0; i < UsedHandles.Count; i++) {
                FreeHandles.Add(UsedHandles[i]);
            }
            UsedHandles.Clear();
        }
    }
}

