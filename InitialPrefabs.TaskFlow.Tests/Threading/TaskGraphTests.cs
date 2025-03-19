using InitialPrefabs.TaskFlow.Collections;
using NUnit.Framework;
using System;

namespace InitialPrefabs.TaskFlow.Threading.Tests {

    public class TaskGraphTests {

        private struct S : ITaskFor {
            public readonly void Execute(int index) { }
        }

        private struct T : ITaskFor {
            public readonly void Execute(int index) { }
        }

        [SetUp]
        public void SetUp() {
            TaskHandleExtensions.Initialize(5);
            var graph = TaskHandleExtensions.Graph;
            var bitArray = new NoAllocBitArray(graph.Bytes.AsByteSpan());
            var index = 0;
            foreach (var element in bitArray) {
                bitArray[index++] = true;
            }

            foreach (var element in bitArray) {
                Assert.That(element, "Did not fill correctly");
            }

            graph.Reset();
            Assert.Multiple(() => {
                Assert.That(graph.Sorted, Has.Count.EqualTo(0),
                        "Sorted elemens were not resetted");
                Assert.That(graph.Nodes, Has.Count.EqualTo(0),
                        "Tracked Nodes were not resetted");

                var bitArray = new NoAllocBitArray(graph.Bytes.AsByteSpan());
                foreach (var element in bitArray) {
                    Assert.That(!element, "Did not reset");
                }
            });
        }

        [Test]
        public void TrackingTaskHandle() {
            var graph = TaskHandleExtensions.Graph;
            graph.Reset();
            Assert.Multiple(() => {
                Assert.That(graph.Nodes, Has.Count.EqualTo(0));
                Assert.That(graph.Metadata, Has.Count.EqualTo(0));
            });

            var handleA = new S { }.ScheduleParallel(128, 32);

            Assert.Multiple(() => {
                Assert.That(graph.Nodes, Has.Count.EqualTo(1));
                Assert.That(graph.Metadata, Has.Count.EqualTo(1));
                var metadata = graph.Metadata[0];
                Console.WriteLine(graph.Metadata.Count);
                Console.WriteLine(metadata.Workload.ToString());

                Assert.That(metadata.Workload.Type, Is.EqualTo(WorkloadType.MultiThreadLoop));
                Assert.That(metadata.Workload.ThreadCount, Is.EqualTo(4));
                Assert.That(metadata.Workload.Total, Is.EqualTo(128));
                Assert.That(metadata.Workload.BatchSize, Is.EqualTo(32));
            });
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
            // F D
            //
            // Or
            // 0 4
            // 1 2
            // 5 3

            var order = new[] { 0, 4, 1, 2, 5, 3 };
            var sorted = TaskHandleExtensions.Graph.Sorted;
            TaskHandleExtensions.Graph.Sort();

            Assert.That(
                sorted,
                Has.Count.EqualTo(order.Length),
                "Mismatched tracking.");

            for (var i = 0; i < order.Length; i++) {
                Assert.That(
                    sorted[i].node.GlobalID,
                    Is.EqualTo(order[i]), $"Failed at {i} with value: {order[i]}, mismatched order");
            }
        }

        [Test]
        public void MetadataTracking() {
            var rand = new Random();
            TaskHandleExtensions.Graph.Reset();
            Span<TaskWorkload> a = stackalloc TaskWorkload[5];
            for (var i = 0; i < 5; i++) {
                var cond = rand.NextSingle() >= 0.5f;
                _ = cond ? new S { }.ScheduleParallel(16, 32) : new S { }.Schedule();

                var metadata = TaskHandleExtensions.Graph.Metadata[i];

                Assert.Multiple(() => {
                    Assert.That(metadata.State, Is.EqualTo(TaskState.NotStarted),
                        "Task should not have been started.");
                    var expected = cond ?
                        new TaskWorkload { BatchSize = 32, Total = 16 } :
                        new TaskWorkload { BatchSize = 0, Total = 1 };
                    Assert.That(
                        TaskHandleExtensions.Graph.Metadata,
                        Has.Count.EqualTo(i + 1),
                        $"Metadata not incremented, failed at {i}");
                });
            }

            _ = new S { }.Schedule();

            Assert.That(
                TaskHandleExtensions.Graph.Metadata,
                Has.Count.EqualTo(6), $"New Metadata has not been reserved");
        }

        [Test]
        public void SortingThrowsErrorOnCyclicDependencies() {
            var handleA = new S { }.Schedule();
            var handleB = new S { }.Schedule(handleA);
            var handleC = new S { }.Schedule(handleB);
            handleA.DependsOn(handleC);

            var exception = Assert.Throws<InvalidOperationException>(static () => {
                TaskHandleExtensions.Graph.Sort();
            });

            Assert.That(exception, Is.Not.Null, "Exception should be thrown");
        }
    }
}

