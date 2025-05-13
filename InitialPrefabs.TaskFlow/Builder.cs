using InitialPrefabs.TaskFlow.Threading;
using InitialPrefabs.TaskFlow.Utils;
using System;

namespace InitialPrefabs.TaskFlow {

    public ref struct Builder {

        private int taskCapacity;
        private int workerCapacity;

        public static Builder Default() {
            return new Builder {
                taskCapacity = TaskConstants.MaxTasks,
                workerCapacity = TaskConstants.MaxTasks
            };
        }

        public Builder WithTaskCapacity(int capacity) {
            taskCapacity = capacity;
            return this;
        }

        public Builder WithWorkerCapacity(int capacity) {
            workerCapacity = capacity;
            return this;
        }

        public readonly Builder WithLogHandler(LogHandler handler) {
            LogUtils.OnLog += handler;
            return this;
        }

        public readonly Builder WithExceptionHandler(ExceptionHandler handler) {
            LogUtils.OnException += handler;
            return this;
        }

        public readonly void Build() {
            if (TaskGraphRunner.Default != null) {
                throw new InvalidOperationException(
                    "Cannot reinitialize the TaskGraph, please dispose the previous one first!");
            }
            if (taskCapacity == 0) {
                throw new InvalidOperationException(
                    "Cannot initialize the TaskGraph with a non positive Task capacity.");
            }
            if (workerCapacity == 0) {
                throw new InvalidOperationException(
                    "Cannot initialize the TaskGraph with a non positive Worker capacity.");
            }
            TaskGraphRunner.UniqueID = 0;
            TaskGraphRunner.Default = new TaskGraph(taskCapacity, workerCapacity);
        }
    }
}
