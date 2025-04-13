using InitialPrefabs.TaskFlow.Collections;
using NUnit.Framework;
using System;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading.Tests {

    public class TaskGraphTests {

        private class RefInt {
            public int Value;
        }

        private struct S : ITaskFor {
            public RefInt RefInt;
            public readonly void Execute(int index) {
                RefInt.Value = Interlocked.Increment(ref RefInt.Value);
            }
        }

        private struct T : ITaskFor {
            public readonly void Execute(int index) { }
        }

        [SetUp]
        public void SetUp() {
            TaskHandleExtensions.Initialize(5);
            var graph = TaskHandleExtensions.Graph;
            var bitArray = new NoAllocBitArray(graph.Bytes.AsSpan());
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

                var bitArray = new NoAllocBitArray(graph.Bytes.AsSpan());
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
        public void SortTest() {
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
                    Is.EqualTo(order[i]),
                    $"Failed at {i} with value: {order[i]}, mismatched order");
            }


            Assert.Multiple(static () => {
                var groups = TaskHandleExtensions.Graph.Groups;
                Assert.That(groups.Length, Is.EqualTo(3),
                    "3 parallel groups should exist.");

                foreach (var e in groups) {
                    Console.WriteLine(e);
                }

                Assert.That(groups[0], Is.EqualTo(new TaskSlice {
                    Start = 0,
                    Count = 2
                }));

                Assert.That(groups[1], Is.EqualTo(new TaskSlice {
                    Start = 2,
                    Count = 2
                }));

                Assert.That(groups[2], Is.EqualTo(new TaskSlice {
                    Start = 4,
                    Count = 2
                }));
            });
        }

        [Test]
        public void ChecksDependency() {
            var value = new RefInt();
            var handleA = new S {
                RefInt = value,
            }.Schedule();
            var handleB = new S {
                RefInt = value,
            }.Schedule(handleA);
            var handleC = new S {
                RefInt = value,
            }.Schedule(handleA);
            var handleD = new S {
                RefInt = value,
            }.Schedule(handleC);
            var handleE = new S {
                RefInt = value,
            }.Schedule();
            var handleF = new S {
                RefInt = value,
            }.Schedule(handleB);

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
                    Is.EqualTo(order[i]),
                    $"Failed at {i} with value: {order[i]}, mismatched order");
            }

            TaskHandleExtensions.Graph.Process();

            Assert.That(value.Value, Is.EqualTo(6),
                "The value should have been incremented.");
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

