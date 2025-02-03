using NUnit.Framework;
using System.Threading.Tasks;

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
        public void Setup() {
        }

        [Test]
        public void Test1() {
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