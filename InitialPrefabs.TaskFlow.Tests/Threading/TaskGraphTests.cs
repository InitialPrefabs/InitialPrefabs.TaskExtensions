using InitialPrefabs.TaskFlow.Collections;
using NUnit.Framework;
using System;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading.Tests {

    public class TaskGraphManagerTests {
        [SetUp]
        public void SetUp() {
            TaskGraphManager.Initialize(5);
            Console.WriteLine($"Count:: {TaskGraphManager.TaskGraphs.Count}");
        }

        [Test]
        public void CreateAndRemoveGraph() {
            Console.WriteLine($"Count:: {TaskGraphManager.TaskGraphs.Count}");
            var graphHandle = TaskGraphManager.CreateGraph();
            Console.WriteLine($"Count:: {TaskGraphManager.TaskGraphs.Count}");
            Assert.That(graphHandle.Ref.Index, Is.EqualTo(1),
                   "There should be 2 graphs");

            Assert.Multiple(static () => {
                Assert.That(TaskGraphManager.TaskGraphs, Has.Count.EqualTo(2));
                Assert.That(TaskGraphManager.TaskHandleMetadata, Has.Count.EqualTo(2));
                Assert.That(TaskGraphManager.Handles, Has.Count.EqualTo(2));
            });

            TaskGraphManager.RemoveGraph(new GraphHandle(0));

            Assert.Multiple(() => {
                Assert.That(TaskGraphManager.TaskGraphs, Has.Count.EqualTo(1));
                Assert.That(TaskGraphManager.TaskHandleMetadata, Has.Count.EqualTo(1));
                Assert.That(TaskGraphManager.Handles, Has.Count.EqualTo(1));

                Assert.That(graphHandle.Ref.Index, Is.EqualTo(0), "The graph's local index should be adjusted.");
            });
        }

        [TearDown]
        public void TearDown() {
            TaskGraphManager.Shutdown();
            var exception = Assert.Throws<IndexOutOfRangeException>(static () => {
                _ = TaskGraphManager.Get();
            });
            Assert.That(exception, Is.Not.Null);
        }
    }

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
            public int[] A;
            public int[] B;

            public readonly void Execute(int index) {
                A[index] = B[index] + 1;
            }
        }

        private struct U : ITaskFor {
            public int[] A;
            public int[] B;
            public readonly void Execute(int index) {
                A[index] = A[index] + B[index];
            }
        }

        private struct V : ITaskFor {
            public int[] A;

            public readonly void Execute(int index) {
                A[index] = index;
            }
        }

        private TaskGraph graph;

        [SetUp]
        public void SetUp() {
            TaskGraphManager.Initialize(5);
            (var graph, var _) = TaskGraphManager.Get(default);
            this.graph = graph;
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

        [TearDown]
        public void TearDown() {
            TaskGraphManager.Shutdown();
        }

        [Test]
        public void TrackingTaskHandle() {
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

                Assert.That(metadata.Workload.Type,
                    Is.EqualTo(WorkloadType.MultiThreadLoop));
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
            var sorted = graph.Sorted;
            graph.Sort();

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


            Assert.Multiple(() => {
                var groups = graph.Groups;
                Assert.That(groups.Length, Is.EqualTo(3),
                    "3 parallel groups should exist.");

                Assert.That(groups[0], Is.EqualTo(new Slice {
                    Start = 0,
                    Count = 2
                }));

                Assert.That(groups[1], Is.EqualTo(new Slice {
                    Start = 2,
                    Count = 2
                }));

                Assert.That(groups[2], Is.EqualTo(new Slice {
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
            var sorted = graph.Sorted;
            graph.Sort();

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

            graph.Process();

            Assert.That(value.Value, Is.EqualTo(6),
                "The value should have been incremented.");
        }

        [Test]
        public void ParallelForTest() {
            var a = new int[8];
            var b = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var handleA = new T {
                A = a,
                B = b
            }.ScheduleParallel(a.Length, 4);

            var _ = new U {
                A = a,
                B = b
            }.ScheduleParallel(a.Length, 4, handleA);

            // 1, 2, 3, 4,  5,  6, 7, 8
            // 2, 3, 4, 5,  6,  7, 8, 9
            // 3, 5, 7, 9, 11, 13, 15, 17
            var c = new int[] { 3, 5, 7, 9, 11, 13, 15, 17 };

            graph.Sort();
            graph.Process();

            for (var i = 0; i < c.Length; i++) {
                Assert.That(a[i], Is.EqualTo(c[i]),
                    $"Failed to parallel add at index: {i}");
            }
        }

        [Test]
        public void MultiParadigmTest() {
            var a = new int[8];
            var b = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };

            // Schedule a loop on a thread
            var handleA = new V {
                A = a
            }.Schedule(8);

            // Schedule 2 loops onto 2 threads
            var handleB = new U {
                A = a,
                B = b
            }.ScheduleParallel(8, 4, handleA);

            graph.Sort();
            graph.Process();

            for (var i = 0; i < 8; i++) {
                Assert.That(a[i], Is.EqualTo(i * 2));
            }
        }

        [Test]
        public void MetadataTracking() {
            var rand = new Random();
            graph.Reset();
            Span<TaskWorkload> a = stackalloc TaskWorkload[5];
            for (var i = 0; i < 5; i++) {
                var cond = rand.NextSingle() >= 0.5f;
                _ = cond ? new S { }.ScheduleParallel(16, 32) : new S { }.Schedule();

                var metadata = graph.Metadata[i];

                Assert.Multiple(() => {
                    Assert.That(metadata.State, Is.EqualTo(TaskState.NotStarted),
                        "Task should not have been started.");
                    var expected = cond ?
                        new TaskWorkload { BatchSize = 32, Total = 16 } :
                        new TaskWorkload { BatchSize = 0, Total = 1 };
                    Assert.That(
                        graph.Metadata,
                        Has.Count.EqualTo(i + 1),
                        $"Metadata not incremented, failed at {i}");
                });
            }

            _ = new S { }.Schedule();

            Assert.That(
                graph.Metadata,
                Has.Count.EqualTo(6), $"New Metadata has not been reserved");
        }

        [Test]
        public void SortingThrowsErrorOnCyclicDependencies() {
            var handleA = new S { }.Schedule();
            var handleB = new S { }.Schedule(handleA);
            var handleC = new S { }.Schedule(handleB);
            handleA.DependsOn(handleC);

            var exception = Assert.Throws<InvalidOperationException>(() => {
                graph.Sort();
            });

            Assert.That(exception, Is.Not.Null, "Exception should be thrown");
        }
    }
}

