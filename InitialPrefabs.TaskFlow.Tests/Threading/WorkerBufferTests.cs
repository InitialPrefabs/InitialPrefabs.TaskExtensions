using NUnit.Framework;
using System;

namespace InitialPrefabs.TaskFlow.Threading.Tests {

    public class WorkerBufferTests {

        private WorkerBuffer buffer;

        [SetUp]
        public void SetUp() {
            buffer = new WorkerBuffer();

            for (var i = 0; i < buffer.Workers.Length; i++) {
                var worker = buffer.Workers[i];
                Assert.That(worker, Is.Not.Null, "Worker not initialized!");
            }

            Assert.That(
                buffer.FreeCounter,
                Is.EqualTo(TaskConstants.MaxTasks),
                "Failed to initialize the worker buffer");
        }

        [TearDown]
        public void TearDown() {
            ((IDisposable)buffer).Dispose();
            for (var i = 0; i < buffer.Workers.Length; i++) {
                var worker = buffer.Workers[i];
                worker.WaitHandle.Dispose();
            }
        }

        private void TestRentOnce((WorkerHandle handle, TaskWorker worker) rented) {
            Assert.Multiple(() => {
                Assert.That(rented.worker, Is.Not.Null, "Invalid worker");
                Assert.That(rented.handle,
                    Is.EqualTo(new WorkerHandle(0)),
                    "Invalid worker handle");

                Assert.That(
                    buffer.Free[0],
                    Is.EqualTo(TaskConstants.MaxTasks - 1),
                    "Failed to swap back from the free list.");
                Assert.That(buffer.FreeCounter,
                    Is.EqualTo(TaskConstants.MaxTasks - 1));

                Assert.That(buffer.UseCounter, Is.EqualTo(1),
                    "The used counter did not increment");
                Assert.That(buffer.Used[0], Is.EqualTo(0),
                    "Worker 0 should be tracked");
            });
        }

        [Test]
        public void RentingAWorker() {
            var rented = buffer.Rent();
            TestRentOnce(rented);
        }

        [Test]
        public void ReturningAWorker() {
            var rented = buffer.Rent();
            TestRentOnce(rented);
            buffer.Return(rented);

            Assert.Multiple(() => {
                Assert.That(buffer.UseCounter, Is.EqualTo(0),
                    "Returning did not reset the use counter.");
                Assert.That(buffer.FreeCounter,
                    Is.EqualTo(TaskConstants.MaxTasks),
                    "Did not return to the free buffer");
            });
        }

        [Test]
        public void TryingToRentTooManyWorkers() {
            for (var i = 0; i < TaskConstants.MaxTasks; i++) {
                _ = buffer.Rent();
            }

            var err = Assert.Throws<InvalidOperationException>(() => {
                _ = buffer.Rent();
            });

            Assert.That(err, Is.Not.Null);
        }
    }
}

