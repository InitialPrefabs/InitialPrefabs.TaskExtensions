using NUnit.Framework;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskExtensions.Tests {

    public class TaskParallelForTests {

        private struct ParallelAdd : ITaskParallelFor {
            public int[] A;
            public int[] B;

            public readonly void Execute(int index) {
                A[index] = A[index] + B[index];
            }
        }

        private struct SingleThreadedAdd : ITask {
            public int[] A;
            public int[] B;

            public readonly void Execute() {
                for (var i = 0; i < A.Length; i++) {
                    A[i] = A[i] + B[i];
                }
            }
        }

        [SetUp]
        public void Setup() {
            TaskHelper.Flush();
            Assert.That(TaskHelper.QueuedTasks, Has.Count.EqualTo(0), "Queued tasks should not exist");
        }

        [TearDown]
        public void Teardown() {
            TaskHelper.Flush();
        }

        [Test]
        public void ParallelAddTest2Threads() {
            var a = new[] { 1, 2, 3, 4 };
            var b = new[] { 1, 2, 3, 4 };

            Assert.That(TaskHelper.QueuedTasks, Has.Count.EqualTo(0), "Queued tasks should not exist");

            var t = new ParallelAdd {
                A = a,
                B = b
            }.Schedule(4, 2);

            t.Wait();

            Assert.That(TaskHelper.QueuedTasks, Has.Count.EqualTo(1), "Failed to create 1 task with combined dependencies");

            for (var i = 0; i < a.Length; i++) {
                var e = a[i];
                Assert.That(e, Is.EqualTo(b[i] * 2), "ParallelAdd did not execute");
            }
        }

        [Test]
        public async Task TaskTrackingForDependencies() {
            var a = new[] { 1, 2, 3, 4 };
            var b = new[] { 1, 2, 3, 4 };
            var c = new[] { 1, 2, 3, 4 };

            Assert.That(TaskHelper.QueuedTasks, Has.Count.EqualTo(0), "Queued tasks should not exist");

            var t1 = new ParallelAdd {
                A = a,
                B = b
            }.Schedule(4, 2);

            var t2 = new ParallelAdd {
                A = a,
                B = c,
            }.Schedule(4, 2);
            await Task.WhenAll(t1, t2);
        }

        [Test]
        public void ParallelAddTest() {
            var a = new[] { 1, 2, 3, 4 };
            var b = new[] { 1, 2, 3, 4 };

            var t = new ParallelAdd {
                A = a,
                B = b
            }.Schedule(4, 4);
            t.Wait();

            Assert.That(TaskHelper.QueuedTasks, Has.Count.EqualTo(1), "Failed to create 1 tasks");

            for (var i = 0; i < a.Length; i++) {
                var e = a[i];
                Assert.That(e, Is.EqualTo(b[i] * 2));
            }
        }

        [Test]
        public void SingleThreadAdd() {
            var a = new[] { 1, 2, 3, 4 };
            var b = new[] { 1, 2, 3, 4 };

            var t = new SingleThreadedAdd {
                A = a,
                B = b
            }.Schedule();
            t.Wait();

            Assert.That(TaskHelper.QueuedTasks, Has.Count.EqualTo(1), "Failed to create 1 tasks");

            for (var i = 0; i < a.Length; i++) {
                var actual = a[i];
                Assert.That(actual, Is.EqualTo(b[i] * 2), "Failed to add a separate thread.");
            }
        }
    }
}
