using InitialPrefabs.TaskFlow.Collections;
using InitialPrefabs.TaskFlow.Utils;
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

    public interface IIndexable<T> {
    }

    public interface ITaskDefPoolable {
        int Remaining { get; }
        int Capacity { get; }

        ITaskUnitRef ElementAt(LocalHandle handle);

        void ReturnHandle(LocalHandle handle);
        void ReturnAllHandles();
    }

    public class TaskDefinitionPool<T0> : ITaskDefPoolable where T0 : struct, ITaskFor {
        internal readonly DynamicArray<TaskUnitRef<T0>> Values;
        internal readonly DynamicArray<ushort> FreeHandles;
        internal readonly DynamicArray<ushort> UsedHandles;

        public TaskDefinitionPool(int capacity) {
            Values = new DynamicArray<TaskUnitRef<T0>>(capacity);
            FreeHandles = new DynamicArray<ushort>(capacity);
            UsedHandles = new DynamicArray<ushort>(capacity);

            for (ushort i = 0; i < capacity; i++) {
                FreeHandles.Add(i);
                Values.Add(new TaskUnitRef<T0>(new T0()));
            }
        }

        public int Remaining => FreeHandles.Count;

        public int Capacity => Values.Capacity;

        public ITaskUnitRef ElementAt(LocalHandle handle) {
            return Values[handle];
        }

        public LocalHandle Rent(T0 value) {
            if (FreeHandles.IsEmpty) {
                var freeHandle = (ushort)Values.Count;
                UsedHandles.Add(freeHandle);
                Values.Add(new TaskUnitRef<T0>(value));
                return new LocalHandle(freeHandle);
            } else {
                var lastIndex = FreeHandles.Count - 1;
                var freeHandle = FreeHandles[lastIndex];
                UsedHandles.Add(freeHandle);
                FreeHandles.RemoveAtSwapback(lastIndex);
                var unitRef = Values[freeHandle];
                unitRef.Task = value;
                return new LocalHandle(freeHandle);
            }
        }

        public void ReturnAllHandles() {
            foreach (var handle in UsedHandles) {
                FreeHandles.Add(handle);
            }
            UsedHandles.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReturnHandle(LocalHandle handle) {
            var idx = UsedHandles.IndexOf((ushort)handle, default(UShortComparer));
            if (idx > -1) {
                UsedHandles.RemoveAtSwapback(idx);
                FreeHandles.Add(handle);
            }
        }
    }

    /// <summary>
    /// Stores structs implement <see cref="ITaskFor"/> to avoid boxing on runtime.
    /// When <see cref="Rent"/> is called, a <see cref="LocalHandle{T0}"/> is returned providing
    /// the index of the <see cref="ITaskFor"/> struct.
    /// </summary>
    public static class TaskUnitPool<T0> where T0 : struct, ITaskFor {

        internal static readonly TaskDefinitionPool<T0> Pool;

        internal static int Capacity => Pool.Capacity;
        internal static int Remaining => Pool.Remaining;

        static TaskUnitPool() {
            var capacity = 5;
            Pool = new TaskDefinitionPool<T0>(capacity);
            TaskGraphRunner.OnReset += Reset;
        }

        /// <summary>
        /// Returns an allocated struct implementing <see cref="ITaskFor"/> to be copied into.
        /// </summary>
        /// <param name="value">The task to perform a copy on into the TaskUnitPool.</param>
        /// <returns>A <see cref="LocalHandle{T0}"/> storing the index and Task container.</returns>
        public static LocalHandle Rent(T0 value) {
            return Pool.Rent(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(LocalHandle handle) {
            Pool.ReturnHandle(handle);
        }

        public static ITaskUnitRef ElementAt(LocalHandle handle) {
            return Pool.ElementAt(handle);
        }

        internal static void Reset() {
            Pool.ReturnAllHandles();
        }
    }
}

