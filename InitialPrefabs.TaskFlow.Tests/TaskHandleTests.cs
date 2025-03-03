using NUnit.Framework;

namespace InitialPrefabs.TaskFlow.Tests {
    public class TaskHandleTests {

        private struct S : ITaskFor {
            public readonly void Execute(int index) { }
        }

        private struct T : ITaskFor {
            public readonly void Execute(int index) { }
        }

        [Test]
        public void ChecksDependency() {
            var graph = new TaskGraph(10);
            var handleA = new S {}.Schedule();
            var handleB = new S {}.Schedule(handleA);
            var handleC = new S {}.Schedule(handleA);
            var handleD = new S {}.Schedule(handleC);
            graph.Sort();
        }
    }
}

