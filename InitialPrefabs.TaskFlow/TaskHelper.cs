using System.Threading.Tasks;

namespace InitialPrefabs.TaskFlow {

    public static class TaskHelper {
        private const int InitialCapacity = 100;

        internal static readonly DynamicArray<Task> QueuedTasks = new DynamicArray<Task>(InitialCapacity);

        public static TaskBuilder CreateTaskBuilder() {
            return new TaskBuilder(QueuedTasks);
        }

        public static void Flush() {
            QueuedTasks.Clear();
        }

        public static Task Schedule<T>(this T task, int total, int workPerTask) where T : struct, ITaskParallelFor {
            var taskBuilder = new TaskBuilder(QueuedTasks);

            var unitsOfWork = Utils.CeilToIntDivision(total, workPerTask);
            for (var i = 0; i < unitsOfWork; i++) {
                var start = i * workPerTask;
                var diff = total - start;
                var length = diff > workPerTask ? workPerTask : diff;
                _ = taskBuilder.AppendParallelTask<T>(task, start, length);
            }

            return taskBuilder.FlattenDependencies();
        }

        public static Task Schedule<T>(this T task) where T : struct, ITask {
            _ = new TaskBuilder(QueuedTasks)
                .AppendTask<T>(task, out var scheduled);
            return scheduled;
        }
    }
}
