using InitialPrefabs.TaskFlow.Threading;
using InitialPrefabs.TaskFlow.Utils;

namespace InitialPrefabs.TaskFlow.Examples {

    internal struct ResetTask : ITaskFor {
        public int[] A;

        public readonly void Execute(int index) {
            A[index] = 0;
        }
    }

    internal struct AddTask : ITaskFor {
        public int[] A;

        public readonly void Execute(int index) {
            A[index] += index;
        }
    }


    public class Program {
        public static void Main(string[] argv) {
            _ = TaskGraphManager.Initialize();
            // LogUtils.OnLog += System.Console.WriteLine;
            var source = new int[100];

            for (var i = 0; i < 10000; i++) {
                using (Profiler.BeginZone("Frame")) {
                    TaskGraphManager.ResetContext();
                    var handle = new AddTask {
                        A = source
                    }.ScheduleParallel(100, 20);

                    _ = new ResetTask {
                        A = source
                    }.ScheduleParallel(100, 20, handle);

                    using (Profiler.BeginZone("TaskGraph Process")) {
                        TaskGraphManager.Process();
                    }
                }

                Profiler.EmitFrameMark();
            }

            TaskGraphManager.Shutdown();
        }
    }
}
