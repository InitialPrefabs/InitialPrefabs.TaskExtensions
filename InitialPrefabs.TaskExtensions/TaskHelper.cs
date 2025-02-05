using System.Threading.Tasks;

namespace InitialPrefabs.TaskExtensions {

    public static class TaskHelper {
        private const int InitialCapacity = 100;
        internal static readonly DynamicArray<Task> QueuedTasks = new DynamicArray<Task>(InitialCapacity);

        public static TaskBuilder CreateTaskBuilder() {
            return new TaskBuilder(QueuedTasks);
        }

        public static void Flush() {
            // Task.WaitAll(QueuedTasks.Collection);
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
                taskBuilder.AppendParallelTask<T>(task, start, length);
            }

            return Task.WhenAll(taskBuilder.GetTasks());
        }
    }
}
