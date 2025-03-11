using InitialPrefabs.TaskFlow.Collections;
using System.Runtime.CompilerServices;

namespace InitialPrefabs.TaskFlow.Threading {

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

        public static ref T0 ElementAt(LocalHandle<T0> handle) {
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

