using NUnit.Framework;

namespace InitialPrefabs.TaskExtensions.Tests {

    public class TaskParallelForTests {
        struct ParllelAdd : ITaskParallelFor {
            public int[] A;
            public int[] B;

            public void Execute(int index) {
                A[index] = A[index] + B[index];
            }
        }

        [SetUp]
        public void Setup()
        {
            TaskHelper.Flush();
            Assert.That(TaskHelper.QueuedTasks.Count == 0, "Queued tasks should not exist");
        }

        [TearDown]
        public void Teardown()
        {

        }

        [Test]
        public void ParallelAddTest2Threads() {
            var a = new [] { 1, 2, 3, 4 };
            var b = new [] { 1, 2, 3, 4 };

            Assert.That(TaskHelper.QueuedTasks.Count == 0, "Queued tasks should not exist");

            var t = new ParllelAdd {
                A = a,
                B = b
            }.Schedule(4, 2);

            t.Wait();

            Assert.That(TaskHelper.QueuedTasks.Count == 2, "Failed to create 2 tasks");

            for (var i = 0; i < a.Length; i++) {
                var e = a[i];
                Assert.That(e == b[i] * 2);
            }
        }

        [Test]
        public void ParallelAddTest() {
            var a = new [] { 1, 2, 3, 4 };
            var b = new [] { 1, 2, 3, 4 };

            var t = new ParllelAdd {
                A = a,
                B = b
            }.Schedule(4, 4);

            t.Wait();

            for (var i = 0; i < a.Length; i++) {
                var e = a[i];
                Assert.That(e == b[i] * 2);
            }
        }
    }
}