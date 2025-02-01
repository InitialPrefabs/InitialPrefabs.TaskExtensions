using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskExtensions {
    public struct TaskSlice : IEnumerable<Task> {

        public readonly IReadOnlyList<Task> QueuedTasks;

        public readonly int Start;
        public readonly int Count;

        public TaskSlice(TaskBuilder taskBuilder) {
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
