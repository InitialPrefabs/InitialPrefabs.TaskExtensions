using NUnit.Framework;
using System;

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
            var handleA = new S { }.Schedule();
            var handleB = new S { }.Schedule(handleA);
            var handleC = new S { }.Schedule(handleA);
            var handleD = new S { }.Schedule(handleC);
            var handleE = new S { }.Schedule();
            var handleF = new S { }.Schedule(handleB);

            // The order should be
            // A E
            // B C
            // D F
            //
            // Or
            // 0 4
            // 1 2
            // 3 5

            var sorted = TaskHandleExtensions.TaskGraph.Sorted;
            TaskHandleExtensions.TaskGraph.Sort();

            foreach (var node in sorted) {
                Console.WriteLine($"TaskHandle ID: {node.GlobalID}");
            }
        }
    }
}

