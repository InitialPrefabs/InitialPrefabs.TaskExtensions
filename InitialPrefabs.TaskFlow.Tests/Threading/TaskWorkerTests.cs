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

        private static void CommonWorkerAssert(TaskWorker worker) {
            Assert.That(worker.Context.ToString(), Is.Not.EqualTo("Empty"));
            Assert.That(worker.Context.IsValid, "After binding, the worker's Context should be valid");
        }

        private struct SumTask : ITaskFor {
            public UnmanagedRef<int> Sum;
            public void Execute(int index) {
                Sum.Ref = 1 + 1;
            }
        }

        [Test]
        public void WaitSingleRewindableTaskUnit() {
            Prepare(ref metadataA, out var refA);
            var sum = 0;

            workerA.Bind(new TaskUnitRef<SumTask>(new SumTask {
                Sum = new UnmanagedRef<int>(ref sum)
            }), refA);

            CommonWorkerAssert(workerA);

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

        private struct ExceptionTask : ITaskFor {
            public void Execute(int index) {
                throw new InvalidOperationException("Forcing a fault");
            }
        }

        [Test]
        public void WaitSingleRewindableTaskUnitWithException() {
            Prepare(ref metadataA, out var refA);
            workerA.Bind(new TaskUnitRef<ExceptionTask>(new ExceptionTask()), refA);
            CommonWorkerAssert(workerA);

            workerA.Start();
            workerA.Wait();

            Assert.That(metadataA.State, Is.EqualTo(TaskState.Faulted),
                "The task should have faulted and earlied out");
        }

        private struct SleepStopwatchTask : ITaskFor {
            public Stopwatch Watch;
            public int Time;
            public readonly void Execute(int index) {
                Watch.Start();
                Thread.Sleep(Time);
                Watch.Stop();
            }
        }

        private struct ParallelForTask : ITaskFor {
            public readonly void Execute(int index) {
                // This will do nothing
            }
        }

        [Test]
        public void WorkerContextCompletionFlagTests() {
            Prepare(ref metadataA, out var refA);
            refA.Ref.Workload = TaskWorkload.MultiUnit(2, 1);
            workerA.Bind(
                0,
                1,
                new TaskUnitRef<ParallelForTask>(new ParallelForTask()),
                refA,
                0);
            CommonWorkerAssert(workerA);

            workerB.Bind(
                1,
                1,
                new TaskUnitRef<ParallelForTask>(new ParallelForTask()),
                refA,
                1);
            CommonWorkerAssert(workerB);

            var workers = new TaskWorker[2] { workerA, workerB };
            TaskWorker.StartAll(workers);
            TaskWorker.WaitAll(workers);

            Assert.That(refA.Ref.CompletionFlags, Is.EqualTo(3), "2 Threads should have written to the completion flags using atomics");
        }

        [Test]
        public void AwaitingMultipleThreads() {
            Prepare(ref metadataA, out var refA);
            Prepare(ref metadataB, out var refB);

            watchA.Start();
            watchB.Start();

            workerA.Bind(new TaskUnitRef<SleepStopwatchTask>(new SleepStopwatchTask {
                Watch = watchA, Time = 100
            }), refA);
            workerB.Bind(new TaskUnitRef<SleepStopwatchTask>(new SleepStopwatchTask {
                Watch = watchB, Time = 200
            }), refB);
            CommonWorkerAssert(workerA);
            CommonWorkerAssert(workerB);

            var array = new TaskWorker[] { workerA, workerB };
            TaskWorker.StartAll(array);
            TaskWorker.WaitAll(array);

            Assert.Multiple(() => {
                Assert.That(watchA.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
                Assert.That(watchB.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(200));
            });
        }

        [Test]
        public void ReusingAWorker() {
            Prepare(ref metadataA, out var refA);

            workerA.Bind(new TaskUnitRef<SleepStopwatchTask>(new SleepStopwatchTask {
                Watch = watchA, Time = 100
            }), refA);
            CommonWorkerAssert(workerA);
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
            workerA.Bind(new TaskUnitRef<SleepStopwatchTask>(new SleepStopwatchTask {
                Watch = watchA, Time = 100
            }), refA);
            workerA.Start();
            workerA.Wait();

            Assert.Multiple(() => {
                Assert.That(watchA.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(100));
                Assert.That(metadataA.State, Is.EqualTo(TaskState.Completed), "Failed to finish the task.");
            });
        }

        [Test]
        public void TaskWorkerWithoutValidContextThrows() {
            var exception = Assert.Throws<InvalidOperationException>(() => { 
                workerA.Context.Bind(0, 1, null, new UnmanagedRef<TaskMetadata>(ref metadataA));
                Assert.That(workerA.Context.IsValid, Is.EqualTo(false));
                Assert.That(workerA.Context.ToString(), Is.EqualTo("Empty"));
                TaskWorker.WorkItemHandler.Invoke(workerA.Context);
            }, "Starting a worker without a valid context should throw.");
            Assert.That(exception, Is.Not.Null, "Should not have an exception.");
        }

        [Test]
        public void StartingATaskThrowsExceptionWhenInFlight() {
            var states = Enum.GetValues(typeof(TaskState)).Cast<TaskState>();
            foreach (var state in states) {
                if (state != TaskState.NotStarted) {
                    Prepare(ref metadataA, out var refA);
                    refA.Ref.State = state;

                    var exception = Assert.Throws<InvalidOperationException>(() => {
                        workerA.Bind(new TaskUnitRef<SleepStopwatchTask>(new SleepStopwatchTask {
                            Watch = watchA, Time = 100
                        }), refA);
                        workerA.Start();
                        workerA.Wait();
                    });

                    Assert.That(exception, Is.Not.Null, "Exception was not thrown!");
                }
            }
        }
    }
}

