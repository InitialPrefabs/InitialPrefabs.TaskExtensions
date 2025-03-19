using InitialPrefabs.TaskFlow.Collections;
using NUnit.Framework;
using System;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading.Tests {

    public class RewindableUnitTaskTests {

        [Test]
        public void WaitSingleRewindableTaskUnit() {
            var rewindable = new RewindableUnitTask();
            var metadata = TaskMetadata.Default();

            var sum = 0;

            rewindable.Start(() => {
                Thread.Sleep(100);
                Console.WriteLine("Finished rewindable");
                sum = 1 + 1;
            }, new UnmanagedRef<TaskMetadata>(ref metadata))
            .Wait();

            Assert.Multiple(() => {
                Assert.That(sum, Is.EqualTo(2),
                    "The thread should have finished executing before unblocking the main thread.");
                Assert.That(metadata.State, Is.EqualTo(TaskState.Completed),
                    "The rewinddable task should have been completed");
                Assert.That(rewindable.Reset(), "Reset should've been called.");

                rewindable.Dispose();
                var exception = Assert.Throws<Exception>(() => {
                    _ = rewindable.Reset();
                });
                Assert.That(exception, Is.Not.Null);
            });
        }

        [Test]
        public void WaitSingleRewindableTaskUnitWithException() {
            var rewindable = new RewindableUnitTask();
            var metadata = TaskMetadata.Default();

            rewindable.Start(static () => {
                throw new InvalidOperationException("Forcing a fault");
            }, new UnmanagedRef<TaskMetadata>(ref metadata))
            .Wait();

            Assert.Multiple(() => {
                Assert.That(metadata.State, Is.EqualTo(TaskState.Faulted),
                    "The task should have faulted and earlied out");
            });
        }

        // TODO: Write tests to check the cancellation workflow and Waiting for multiple tasks to complete before executing a new one.
        // TODO: Write a test for reusing the same RewindableUnitTask
    }
}

