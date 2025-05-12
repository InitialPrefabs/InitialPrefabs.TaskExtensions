using InitialPrefabs.TaskFlow.Utils;
using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public static class TaskGraphRunner {

        internal delegate void ResetHandler();

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
                UniqueID = 0;
                TaskGraphRunner.Default = new TaskGraph(taskCapacity, workerCapacity);
            }
        }

        public static TaskGraph Graph => Default;
        private static ResetHandler _ResetHandler;

        internal static event ResetHandler OnReset {
            add {
                _ResetHandler -= value;
                _ResetHandler += value;
            }
            remove =>
                _ResetHandler -= value;
        }

        internal static TaskGraph Default;
        internal static ushort UniqueID;

        public static void Reset() {
            UniqueID = 0;
            Default.Reset();
        }

        public static void Update() {
            Default.Sort();
            Default.Process();
        }

        public static void Shutdown() {
            Default.Reset();
            Default = null;
            UniqueID = 0;
        }
    }
}
