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
        }
    }
}
