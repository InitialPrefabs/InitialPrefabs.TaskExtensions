using InitialPrefabs.TaskFlow.Collections;
using System;
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

        public static TaskHandle<T0> Schedule<T0>(this T0 task, TaskHandle<T0> dependsOn) where T0 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent();
            ref var metadata = ref TaskUnitPool<T0>.ElementAt(handle);
            metadata.Reset();
            metadata.Task = task;
            metadata.Workload = new Workload {
                ThreadCount = 1,
                WorkDonePerThread = 0
            };

            // TODO: Track the task handle (will have to do boxing)
            return new TaskHandle<T0>(handle) {
                Dependencies = new FixedUInt16Array32 {
                    dependsOn.Handle
                }
            };
        }

        public static TaskHandle<T0> Schedule<T0>(this T0 task, Span<TaskHandle<T0>> dependsOn) where T0 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent();
            ref var metadata = ref TaskUnitPool<T0>.ElementAt(handle);

            metadata.Reset();
            metadata.Task = task;
            metadata.Workload = new Workload {
                ThreadCount = 1,
                WorkDonePerThread = 0
            };

            var dependencies = new FixedUInt16Array32();
            for (var i = 0; i < dependsOn.Length; i++) {
                dependencies.Add(dependsOn[i].Handle);
            }

            // TODO: Track the task handle (will have to do boxing)
            return new TaskHandle<T0>(handle) {
                Dependencies = dependencies
            };
        }

        /*
        public static Task Schedule<T>(this T task, int total, int workPerTask) where T : struct, ITaskFor {
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
        */

        /*
        public static Task Schedule<T>(this T task) where T : struct, ITask {
            _ = new TaskBuilder(QueuedTasks)
                .AppendTask<T>(task, out var scheduled);
            return scheduled;
        }
        */
    }
}
