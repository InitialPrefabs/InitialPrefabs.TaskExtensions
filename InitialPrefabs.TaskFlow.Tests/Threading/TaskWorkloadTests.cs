using NUnit.Framework;

namespace InitialPrefabs.TaskFlow.Threading.Tests {
    public class TaskWorkloadTests {

        [Test]
        public void SingleUnitNoLoopTest() {
            var workload = TaskWorkload.SingleUnit();

            Assert.Multiple(() => {
                Assert.That(workload.Type,
                    Is.EqualTo(WorkloadType.SingleThreadNoLoop));
                Assert.That(workload.ThreadCount,
                    Is.EqualTo(1), "Only 1 thread should spawn for a SingleUnit.");
            });
        }

        [Test]
        public void SingleUnitLoopTest() {
            var workload = TaskWorkload.LoopedSingleUnit(32);

            Assert.Multiple(() => {
                Assert.That(workload.Type,
                    Is.EqualTo(WorkloadType.SingleThreadLoop));
                Assert.That(workload.ThreadCount,
                    Is.EqualTo(1),
                    "Only 1 thread should spawn for a SingleLoopedUnit.");
            });
        }

        [Test]
        public void MultiUnitLoopTestEquals() {
            var workload = TaskWorkload.MultiUnit(128, 32);

            Assert.Multiple(() => {
                Assert.That(workload.Type,
                    Is.EqualTo(WorkloadType.MultiThreadLoop));
                Assert.That(workload.ThreadCount,
                    Is.EqualTo(4),
                    "4 threads should spawn for the multi unit work");
            });
        }

        [Test]
        public void MultiUnitLoopTestLessEquals() {
            var workload = TaskWorkload.MultiUnit(127, 32);

            Assert.Multiple(() => {
                Assert.That(workload.Type,
                    Is.EqualTo(WorkloadType.MultiThreadLoop));
                Assert.That(workload.ThreadCount,
                    Is.EqualTo(3),
                    "3 threads should spawn for the multi unit work");
            });
        }

        [Test]
        public void MultiUnitLoopTestMoreEquals() {
            var workload = TaskWorkload.MultiUnit(129, 32);

            Assert.Multiple(() => {
                Assert.That(workload.Type,
                    Is.EqualTo(WorkloadType.MultiThreadLoop));
                Assert.That(workload.ThreadCount,
                    Is.EqualTo(4),
                    "3 threads should spawn for the multi unit work");
            });
        }
    }
}

