using InitialPrefabs.TaskFlow.Threading;
using InitialPrefabs.TaskFlow.Utils;
using System;

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
            A[index] += 1;
        }
    }

    public class Program {
        public static void Main(string[] argv) {
            Profiler.AppInfo("Sample App");
            new TaskGraphRunner.Builder()
                .WithTaskCapacity(64)
                .Build();

            LogUtils.OnLog += System.Console.WriteLine;
            var source = new int[100];

            for (var i = 0; i < 10000; i++) {
                Console.WriteLine($"Iteration: {i}");
                using (Profiler.BeginZone("Frame")) {
                    TaskGraphRunner.Reset();

                    var handle = new ResetTask {
                        A = source
                    }.ScheduleParallel(100, 20);

                    _ = new AddTask {
                        A = source
                    }.ScheduleParallel(100, 20, handle);

                    using (Profiler.BeginZone("TaskGraph Process")) {
                        TaskGraphRunner.Update();
                    }
                    var sum = 0;
                    foreach (var v in source) {
                        sum += v;
                    }
                    Console.WriteLine($"Sum: {sum}");
                }

                Profiler.EmitFrameMark();
            }

            TaskGraphRunner.Shutdown();
        }
    }
}
