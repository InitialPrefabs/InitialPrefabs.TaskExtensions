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
        internal static readonly DynamicArray<ushort> FreeHandles;
        internal static readonly DynamicArray<ushort> UsedHandles;

        public static int Remaining => FreeHandles.Count;
        public static int Capacity => Tasks.Capacity;

        static TaskUnitPool() {
            var capacity = 5;
            Tasks = new DynamicArray<T0>(capacity);
            FreeHandles = new DynamicArray<ushort>(capacity);
            UsedHandles = new DynamicArray<ushort>(capacity);

            for (ushort i = 0; i < 5; i++) {
                FreeHandles.Add(i);
                Tasks.Add(new T0());
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
                Tasks.Add(value);
                return new LocalHandle(freeHandle);
            } else {
                var lastIndex = FreeHandles.Count - 1;
                var freeHandle = FreeHandles[lastIndex];
                UsedHandles.Add(freeHandle);
                FreeHandles.RemoveAtSwapback(lastIndex);
                Tasks[freeHandle] = value;
                return new LocalHandle(freeHandle);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(LocalHandle handle) {
            FreeHandles.Add(handle);
        }

        public static ref T0 ElementAt(LocalHandle handle) {
            return ref Tasks.Collection[handle];
        }

        public static void Reset() {
            foreach (var element in UsedHandles) {
                FreeHandles.Add(element);
            }
            UsedHandles.Clear();
        }
    }
}

