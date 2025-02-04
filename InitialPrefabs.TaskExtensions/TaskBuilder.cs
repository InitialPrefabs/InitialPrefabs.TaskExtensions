using System.Collections.Generic;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskExtensions {
    public readonly ref struct TaskBuilder {
        public readonly List<Task> QueuedTasks;
        public readonly int StartOffset;
        public int TotalCount => QueuedTasks.Count;

        public TaskBuilder(List<Task> queue) {
            QueuedTasks = queue;
            StartOffset = queue.Count;
        }

        public TaskBuilder AppendTask<T>(ITaskParallelFor taskParallel, int start, int length) where T : struct, ITaskParallelFor {
            var task = Task.Factory.StartNew(() => {
                for (var i = 0; i < length; i++) {
                    taskParallel.Execute(i + start);
                }
            });
            QueuedTasks.Add(task);
            return this;
        }

        public IEnumerable<Task> GetTasks() {
            return new TaskSlice(this);
        }
    }
}
