using InitialPrefabs.TaskFlow.Collections;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading.Tests {

    public class TaskWorkerTests {

        private TaskWorker workerA;
        private TaskMetadata metadataA;

        private TaskWorker workerB;
        private TaskMetadata metadataB;

        private Stopwatch watchA;
        private Stopwatch watchB;

        [SetUp]
        public void SetUp() {
            workerA = new TaskWorker();
            metadataA = new TaskMetadata();

            workerB = new TaskWorker();
            metadataB = new TaskMetadata();

            watchA = new Stopwatch();
            watchB = new Stopwatch();
        }

        [TearDown]
        public void TearDown() {
            metadataA.Reset();
            metadataB.Reset();
            watchA.Reset();
            watchB.Reset();
            Dispose();
        }

        private void Dispose() {
            try {
                workerA.Dispose();
            } catch (Exception) { }

            try {
                workerB.Dispose();
            } catch (Exception) { }
        }

        private static void Prepare(ref TaskMetadata m, out UnmanagedRef<TaskMetadata> refM) {
            refM = new UnmanagedRef<TaskMetadata>(ref m);
        }

        [Test]
        public void WaitSingleRewindableTaskUnit() {
            Prepare(ref metadataA, out var refA);
            var sum = 0;

            workerA.Bind(() => {
                Thread.Sleep(100);
                Console.WriteLine("Finished rewindable");
                sum = 1 + 1;
            }, refA, -1, 0);
            workerA.Start();
            workerA.Wait();

            Assert.Multiple(() => {
                Assert.That(sum, Is.EqualTo(2),
                    "The thread should have finished executing before unblocking the main thread.");
                Assert.That(metadataA.State, Is.EqualTo(TaskState.Completed),
                    "The rewinddable task should have been completed");
                Assert.That(workerA.Reset(), "Reset should've been called.");
            });
        }

        [Test]
        public void WaitSingleRewindableTaskUnitWithException() {
            Prepare(ref metadataA, out var refA);
            workerA.Bind(static () => {
                throw new InvalidOperationException("Forcing a fault");
            }, refA, -1, 0);
            workerA.Start();
            workerA.Wait();

            Assert.That(metadataA.State, Is.EqualTo(TaskState.Faulted),
                "The task should have faulted and earlied out");
        }

        [Test]
        public void AwaitingMultipleThreads() {
            Prepare(ref metadataA, out var refA);
            Prepare(ref metadataB, out var refB);

            watchA.Start();
            watchB.Start();

            workerA.Bind(() => {
                Thread.Sleep(100);
                watchA.Stop();
            }, refA, -1, 0);

            workerB.Bind(() => {
                Thread.Sleep(200);
                watchB.Stop();
            }, refB, -1, 0);

            workerA.Start();
            workerB.Start();

            var array = new TaskWorker[] { workerA, workerB };
            TaskWorker.WaitAll(array);

            Assert.Multiple(() => {
                Assert.That(watchA.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
                Assert.That(watchB.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(200));
            });
        }

        [Test]
        public void ReusingAWorker() {
            Prepare(ref metadataA, out var refA);

            workerA.Bind(() => {
                watchA.Start();
                Thread.Sleep(100);
                watchA.Stop();
            }, refA, -1, 0);
            workerA.Start();
            workerA.Wait();

            Assert.Multiple(() => {
                Assert.That(watchA.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
                Assert.That(metadataA.State, Is.EqualTo(TaskState.Completed), "Failed to finish the task.");
            });

            // Reset the task state
            refA.Ref.Reset();
            Assert.Multiple(() => {
                Assert.That(refA.Ref.State, Is.EqualTo(TaskState.NotStarted));
            });

            _ = workerA.Reset();

            workerA.Bind(() => {
                watchA.Restart();
                Thread.Sleep(100);
                watchA.Stop();
            }, refA, -1, 0);
            workerA.Start();
            workerA.Wait();

            Assert.Multiple(() => {
                Assert.That(watchA.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
                Assert.That(metadataA.State, Is.EqualTo(TaskState.Completed), "Failed to finish the task.");
            });
        }

        [Test]
        public void StartingATaskThrowsExceptionWhenInFlight() {
            var states = Enum.GetValues(typeof(TaskState)).Cast<TaskState>();
            foreach (var state in states) {
                if (state != TaskState.NotStarted) {
                    Prepare(ref metadataA, out var refA);
                    refA.Ref.State = state;

                    var exception = Assert.Throws<InvalidOperationException>(() => {
                        workerA.Bind(static () => {
                            Thread.Sleep(100);
                        }, refA, -1, 0);
                        workerA.Start();
                        workerA.Wait();
                    });

                    Assert.That(exception, Is.Not.Null, "Exception was not thrown!");
                }
            }
        }
    }
}

