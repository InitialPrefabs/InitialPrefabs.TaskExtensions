using NUnit.Framework;
using System;

namespace InitialPrefabs.TaskFlow.Collections.Tests {

    public class TaskPoolTests {

        private struct S : ITaskFor, IEquatable<S> {
            public int Value;

            public readonly bool Equals(S other) {
                return other.Value == Value;
            }

            public readonly void Execute(int index) { }
        }

        private DynamicArray<Handle<S>> handles;

        [SetUp]
        public void SetUp() {
            handles = new DynamicArray<Handle<S>>(5);
            TaskPool<S>.Reset();
        }

        [Test]
        public void TaskPoolRenting() {
            var handle = TaskPool<S>.Rent();
            Assert.Multiple(() => {
                Assert.That(handle.Task, Is.EqualTo(default(S)), "Task returned is not the default for first try.");
                Assert.That(handle.Index, Is.EqualTo(TaskPool<S>.Capacity - 1), "Handle Index is not correct.");
            });
            var handles = new DynamicArray<Handle<S>>(5) { handle };

            Assert.Multiple(() => {
                var remaining = TaskPool<S>.Remaining;
                Assert.That(remaining, Is.EqualTo(TaskPool<S>.Capacity - 1), "Failed to rent, the RemainingCount did not decrement.");

                for (var i = 0; i < remaining; i++) {
                    handles.Add(TaskPool<S>.Rent());
                }
                Assert.That(TaskPool<S>.Remaining, Is.EqualTo(0), "Did not fully rent out.");
                Assert.That(handles, Has.Count.EqualTo(TaskPool<S>.Capacity));
            });

            var index = 0;
            foreach (var h in handles) {
                TaskPool<S>.Return(h);
                index++;
                Assert.That(TaskPool<S>.Remaining, Is.EqualTo(index), "Did not successfully return to the TaskPool.");
            }
        }

        [Test]
        public void TaskPoolNewAllocation() {
            var remaining = TaskPool<S>.Remaining;
            for (var i = 0; i < remaining; i++) {
                var handle = TaskPool<S>.Rent();
                handles.Add(handle);
            }

            Assert.That(TaskPool<S>.Remaining, Is.EqualTo(0), "Did not empty!");

            handles.Add(TaskPool<S>.Rent());
            Assert.That(TaskPool<S>.Tasks, Has.Count.EqualTo(6), "Did not create a new Task to be stored.");
        }
    }
}
