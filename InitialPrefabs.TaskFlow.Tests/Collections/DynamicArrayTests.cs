using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskFlow.Collections.Tests {

    public class DynamicArrayTests {

        private struct IntComparer : IComparer<int> {
            public readonly int Compare(int x, int y) {
                return x.CompareTo(y);
            }
        }

        [Test]
        public void RemovingTheFirstElementInADynamicArray() {
            var array = new DynamicArray<int>(3);
            for (var i = 0; i < 3; i++) {
                array.Add(i + 1);
            }
            array.RemoveAt(0);
            Assert.Multiple(() => {
                Assert.That(array, Has.Count.EqualTo(2));
                for (var i = 0; i < 2; i++) {
                    Assert.That(array[i], Is.EqualTo(i + 2));
                }
            });
        }

        [Test]
        public void ArrayInitialized() {
            var array = new DynamicArray<int>(10);
            Assert.Multiple(() => {
                Assert.That(array.Collection,
                        Is.Not.Null,
                        "Array not initalized!");
                Assert.That(array.Capacity,
                        Is.EqualTo(10),
                        "Array not initialized to the correct size");
                Assert.That(array,
                        Has.Count.EqualTo(0),
                        "Array is filled");
            });
        }

        [Test]
        public void DynamicArrayFilled() {
            var array = new DynamicArray<int>(10);
            array.Add(1);

            Assert.That(
                array,
                Has.Count.EqualTo(1),
                "Value not added");
            Assert.That(array[^1],
                    Is.EqualTo(1),
                    "Value was not correctly stored");

            var span = array.AsReadOnlySpan();
            foreach (var e in span) {
                Assert.That(e,
                    Is.EqualTo(1),
                    "ReadOnlySpan was not correctly added");
            }
        }

        [Test]
        public void DynamicArrayFilledAndRemove() {
            var array = new DynamicArray<int>(10);
            for (var i = 0; i < 11; i++) {
                // System.Console.WriteLine(i);
                array.Add(i);
            }
            Assert.That(array.Capacity,
                    Is.EqualTo(11),
                    "Capacity was not resized during the push operation");

            array.ForceResize(20);
            Assert.That(
                    array.Capacity,
                    Is.EqualTo(20),
                    "Resize operation was not valid");

            array.RemoveAt(0);
            Assert.Multiple(() => {
                Assert.That(array,
                        Has.Count.EqualTo(10),
                        "Value was not removed from the beginning of collection");
                Assert.That(array[0],
                        Is.EqualTo(1),
                        "The value was not shifted up.");
            });

            array.RemoveAtSwapback(0);
            Assert.Multiple(() => {
                Assert.That(array[0],
                        Is.EqualTo(10),
                        "The last element should be swapped with the first element and removed.");
                Assert.That(array,
                        Has.Count.EqualTo(9),
                        "RemoveAtSwapback did not properly remove from an element");
            });

            array.RemoveAtSwapback(array.Count - 1);
            Assert.Multiple(() => {
                Assert.That(array,
                        Has.Count.EqualTo(8),
                        "Swapback failed at the final element.");
                Assert.That(array[^1],
                        Is.EqualTo(8), "The last element should be 7.");
            });
        }

        [Test]
        public void ResizeTests() {
            var array = new DynamicArray<int>(10);
            for (var i = 0; i < 5; i++) {
                array.Add(i);
            }

            Assert.That(array, Has.Count.EqualTo(5), "Not added to array.");
            array.ForceResize(20, ResizeType.ResetCount);
            Assert.That(array, Is.Empty, "Count did not reset");

            array.ForceResize(10, ResizeType.ForceCount);
            Assert.That(array, Has.Count.EqualTo(10), "Did not force count to the capacity");

            array.ForceResize(30);
            Assert.That(array, Has.Count.EqualTo(10), "Did not force count to the capacity");

            array.ForceResize(20, (ResizeType)4);
            Assert.That(array, Has.Count.EqualTo(10), "Did not force count to the capacity");
        }

        [Test]
        public void FindingElementInArray() {
            var array = new DynamicArray<int>(10, -1);
            for (var i = 0; i < 10; i++) {
                array.Add(i);
            }

            var idx = array.IndexOf(11, default(IntComparer));
            Assert.That(idx, Is.EqualTo(-1), "Should not have found the value -11");
            idx = array.IndexOf(4, default(IntComparer));
            Assert.That(idx, Is.EqualTo(4), "Should not have found the value -11");
        }

        [Test]
        public void AddingToCollectionFromMultipleThreads() {
            var array = new DynamicArray<int>(10);
            var taskA = Task.Factory.StartNew(() => {
                array.AsyncAdd(1);
            });

            var taskB = Task.Factory.StartNew(() => {
                array.AsyncAdd(10);
            });

            Task.WaitAll(taskA, taskB);

            Assert.Multiple(() => {
                Assert.That(array, Has.Count.EqualTo(2),
                    "2 elements should've been added to the dynamic array");

                var actual = array.Find(x => x == 1);
                Assert.That(actual, Is.GreaterThanOrEqualTo(0), "The value 1 should've been added");
                actual = array.Find(x => x == 10);
                Assert.That(actual, Is.GreaterThanOrEqualTo(0), "The value 10 should've been added");
            });
        }
    }
}
