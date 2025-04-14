using InitialPrefabs.TaskFlow.Threading;
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
            A[index] += index;
        }
    }

    public class Program {
        public static void Main(string[] argv) {
            TaskHandleExtensions.Initialize(5);

            var frame = 0;
            var a = new int[100];
            for (var i = 0; i < 100; i++) {
                TaskHandleExtensions.Graph.Reset();
                TaskHandleExtensions.Reset();

                Console.WriteLine($"Frame: {frame}");
                var handleA = new ResetTask {
                    A = a
                }.Schedule(100);

                var _ = new AddTask {
                    A = a
                }.ScheduleParallel(a.Length, 10, handleA);

                TaskHandleExtensions.Graph.Sort();
                TaskHandleExtensions.Graph.Process();

                foreach (var e in a) {
                    Console.WriteLine(e);
                }
                frame++;
            }
        }
    }
}
