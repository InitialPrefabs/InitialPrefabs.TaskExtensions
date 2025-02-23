using InitialPrefabs.TaskFlow.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskFlow {
    public readonly struct TaskBuilder {
        internal readonly DynamicArray<Task> QueuedTasks;

        public readonly int StartOffset;
        public int TotalCount => QueuedTasks.Count;

        internal TaskBuilder(DynamicArray<Task> queue) {
            QueuedTasks = queue;
            StartOffset = queue.Count;
        }

        public readonly TaskBuilder AppendParallelTask<T>(ITaskParallelFor taskParallel, int start, int length) where T : struct, ITaskParallelFor {
            var task = Task.Factory.StartNew(() => {
                for (var i = 0; i < length; i++) {
                    taskParallel.Execute(i + start);
                }
            });

            QueuedTasks.Push(task);
            return this;
        }

        public readonly TaskBuilder AppendTask<T>(ITask task, out Task scheduledTask) where T : struct, ITask {
            scheduledTask = Task.Factory.StartNew(task.Execute);
            QueuedTasks.Push(scheduledTask);
            return this;
        }

        public IEnumerable<Task> GetTasks() {
            return new TaskSlice(this);
        }

        public readonly Task FlattenDependencies() {
            var slice = GetTasks();
            var combined = Task.WhenAll(slice);
            for (var i = TotalCount - 1; i >= StartOffset; i--) {
                QueuedTasks.RemoveAtSwapback(i);
            }
            QueuedTasks.Push(combined);
            return combined;
        }
    }
}
