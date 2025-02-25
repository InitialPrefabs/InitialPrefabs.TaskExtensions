using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow.Collections {

    public readonly struct Handle<T0> where T0 : struct, ITaskFor {
        public readonly ushort Index;
        public readonly T0 Task;

        public Handle(ushort index, T0 task) {
            Index = index;
            Task = task;
        }
    }

    /// <summary>
    /// Stores structs implement <see cref="ITaskFor"/> to avoid boxing on runtime.
    /// When <see cref="Rent"/> is called, a <see cref="Handle{T0}"/> is returned providing
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
                Tasks.Add(default);
            }
        }

        /// <summary>
        /// Returns an allocated struct implementing <see cref="ITaskFor"/> to be copied into,
        /// </summary>
        /// <returns>A <see cref="Handle{T0}"/> storing the index and Task container.</returns>
        public static Handle<T0> Rent() {
            if (FreeIndices.IsEmpty) {
                var task = default(T0);
                var index = (ushort)Tasks.Count;
                Tasks.Add(task);
                return new Handle<T0>(index, task);
            } else {
                var lastIndex = FreeIndices.Count - 1;
                var freeIndex = FreeIndices[lastIndex];
                FreeIndices.RemoveAtSwapback(lastIndex);
                var task = Tasks[freeIndex];
                return new Handle<T0>(freeIndex, task);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(Handle<T0> handle) {
            FreeIndices.Add(handle.Index);
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

