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
            for (var i = 0; i < length; i++) {
                var offset = start + i;
                var task = Task.Factory.StartNew(() => {
                    taskParallel.Execute(offset);
                });
                QueuedTasks.Add(task);
            }
            return this;
        }

        public IEnumerable<Task> GetTasks() {
            return new TaskSlice(this);
        }
    }
}
