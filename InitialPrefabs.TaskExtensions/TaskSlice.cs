using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskExtensions {

    /// <summary>
    /// Represents a readonly slice of the <see cref="Task"/> from a <see cref="TaskBuilder"/>.
    /// </summary>
    public readonly struct TaskSlice : IEnumerable<Task> {
        private readonly DynamicArray<Task> QueuedTasks;
        private readonly int Start;
        private readonly int Count;

        internal TaskSlice(TaskBuilder taskBuilder) {
            Start = taskBuilder.StartOffset;
            Count = taskBuilder.TotalCount - Start;
            QueuedTasks = taskBuilder.QueuedTasks;
        }

        public IEnumerator<Task> GetEnumerator() {
            for (var i = 0; i < Count; i++) {
                var offset = Start + i;
                yield return QueuedTasks[offset];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
