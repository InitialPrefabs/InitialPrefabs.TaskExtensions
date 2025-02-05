using System.Collections.Generic;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskExtensions {
    public readonly struct TaskBuilder {
        internal readonly DynamicArray<Task> QueuedTasks;
        public readonly int StartOffset;
        public int TotalCount => QueuedTasks.Count;

        internal TaskBuilder(DynamicArray<Task> queue) {
            QueuedTasks = queue;
            StartOffset = queue.Count;
        }

        public TaskBuilder AppendParallelTask<T>(ITaskParallelFor taskParallel, int start, int length) where T : struct, ITaskParallelFor {
            var task = Task.Factory.StartNew(() => {
                for (var i = 0; i < length; i++) {
                    taskParallel.Execute(i + start);
                }
            });
            QueuedTasks.Push(task);
            return this;
        }

        public IEnumerable<Task> GetTasks() {
            return new TaskSlice(this);
        }
    }
}
