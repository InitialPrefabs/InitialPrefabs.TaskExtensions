using InitialPrefabs.TaskFlow.Collections;
using NUnit.Framework;
using System;

namespace InitialPrefabs.TaskFlow.Threading.Tests {

    public class TaskUnitPoolTests {

        private struct S : ITaskFor, IEquatable<S> {
            public int Value;

            public readonly bool Equals(S other) {
                return other.Value == Value;
            }

            public readonly void Execute(int index) { }
        }

        private struct A : ITaskFor {
            public void Execute(int index) {
                throw new NotImplementedException();
            }
        }

        private readonly DynamicArray<LocalHandle> handles = new DynamicArray<LocalHandle>(5);

        [SetUp]
        public void SetUp() {
            handles.Clear();
            TaskUnitPool<S>.Reset();
        }

        [Test]
        public void TaskPoolRenting() {
            var handle = TaskUnitPool<S>.Rent(new S { Value = 0 });
            var handleA = TaskUnitPool<A>.Rent(new A());
            Assert.That((ushort)handle,
                    Is.EqualTo(TaskUnitPool<S>.Capacity - 1),
                    "Handle Index is not correct.");
            var handles = new DynamicArray<LocalHandle>(5) { handle };

            Assert.Multiple(() => {
                var remaining = TaskUnitPool<S>.Remaining;
                Assert.That(remaining,
                        Is.EqualTo(TaskUnitPool<S>.Capacity - 1),
                        "Failed to rent, the RemainingCount did not decrement.");

                for (var i = 0; i < remaining; i++) {
                    handles.Add(TaskUnitPool<S>.Rent(new S { Value = 0 }));
                }
                Assert.That(TaskUnitPool<S>.Remaining,
                        Is.EqualTo(0),
                        "Did not fully rent out.");
                Assert.That(handles,
                        Has.Count.EqualTo(TaskUnitPool<S>.Capacity));
            });

            var index = 0;
            foreach (var h in handles) {
                TaskUnitPool<S>.Return(h);
                index++;
                Assert.That(TaskUnitPool<S>.Remaining,
                        Is.EqualTo(index),
                        "Did not successfully return to the TaskPool.");
            }
        }

        [Test]
        public void TaskPoolNewAllocation() {
            var remaining = TaskUnitPool<S>.Remaining;
            for (var i = 0; i < remaining; i++) {
                var handle = TaskUnitPool<S>.Rent(new S { Value = 0 });
                handles.Add(handle);
            }

            Assert.That(TaskUnitPool<S>.Remaining,
                    Is.EqualTo(0),
                    "Did not empty!");

            handles.Add(TaskUnitPool<S>.Rent(new S { Value = 0 }));
            Assert.That(TaskUnitPool<S>.Tasks,
                    Has.Count.EqualTo(6),
                    "Did not create a new Task to be stored.");
        }

        [Test]
        public void AccessingTaskViaHandle() {
            var remaining = TaskUnitPool<S>.Remaining;
            for (var i = 0; i < remaining; i++) {
                var handle = TaskUnitPool<S>.Rent(new S { Value = 0 });
                handles.Add(handle);
            }

            foreach (var handle in handles) {
                ref var task = ref TaskUnitPool<S>.ElementAt(handle);
                var t = (TaskUnitRef<S>)task;
                Assert.That(t.Task, Is.EqualTo(default(S)));
            }
        }
    }
}
