using System.Collections.Generic;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskExtensions {

    public static class TaskHelper {
        private static readonly List<Task> QueuedTasks = new List<Task>(100);

        public static TaskBuilder CreateTaskBuilder() {
            return new TaskBuilder(QueuedTasks);
        }

        public static void Flush() {
            // TODO: Check the cancellation token status
            QueuedTasks.Clear();
        }

        public static Task Schedule<T>(this T task, int total, int workPerTask) where T : struct, ITaskParallelFor {
            var taskBuilder = new TaskBuilder(QueuedTasks);

            var unitsOfWork = Utils.CeilToIntDivision(total, workPerTask);
            for (var i = 0; i < unitsOfWork; i++) {
                var start = i * workPerTask;
                var diff = total - start;
                var length = diff > workPerTask ? workPerTask : diff;
                taskBuilder.AppendTask<T>(task, start, length);
            }

            return Task.WhenAll(taskBuilder.GetTasks());
        }
    }
}
