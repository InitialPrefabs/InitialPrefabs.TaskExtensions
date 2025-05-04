using InitialPrefabs.TaskFlow.Threading;

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
            var graph = TaskGraphManager.Initialize();
            var source = new int[100];

            for (var i = 0; i < 100; i++) {
                var handle = new AddTask {
                    A = source
                }.ScheduleParallel(100, 20);

                _ = new ResetTask {
                    A = source
                }.ScheduleParallel(100, 20, handle);

                graph.Sort();
                graph.Process();
            }

            TaskGraphManager.Shutdown();
        }
    }
}
