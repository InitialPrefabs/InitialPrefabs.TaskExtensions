using NUnit.Framework;

namespace InitialPrefabs.TaskFlow.Collections.Tests {
    public class DynamicArrayTests {

        [Test]
        public void ArrayInitialized() {
            var array = new DynamicArray<int>(10);
            Assert.Multiple(() => {
                Assert.That(array.Collection, Is.Not.Null, "Array not initalized!");
                Assert.That(array.Capacity, Is.EqualTo(10), "Array not initialized to the correct size");
                Assert.That(array, Has.Count.EqualTo(0), "Array is filled");
            });
        }

        [Test]
        public void DynamicArrayFilled() {
            var array = new DynamicArray<int>(10) {
                1
            };

            Assert.That(array, Has.Count.EqualTo(1), "Value not added");
            Assert.That(array[^1], Is.EqualTo(1), "Value was not correctly stored");

            var span = array.AsReadOnlySpan();
            foreach (var e in span) {
                Assert.That(e, Is.EqualTo(1), "ReadOnlySpan was not correctly added");
            }
        }

        [Test]
        public void DynamicArrayFilledAndRemove() {
            var array = new DynamicArray<int>(10);
            for (var i = 0; i < 11; i++) {
                array.Add(i);
            }
            Assert.That(array.Capacity, Is.EqualTo(11), "Capacity was not resized during the push operation");

            array.ForceResize(20);
            Assert.That(array.Capacity, Is.EqualTo(20), "Resize operation was not valid");

            array.RemoveAt(0);
            Assert.Multiple(() => {
                Assert.That(array, Has.Count.EqualTo(10), "Value was not removed from the beginning of collection");
                Assert.That(array[0], Is.EqualTo(1), "The value was not shifted up.");
            });

            array.RemoveAtSwapback(0);
            Assert.Multiple(() => {
                Assert.That(array[0], Is.EqualTo(10), "The last element should be swapped with the first element and removed.");
                Assert.That(array, Has.Count.EqualTo(9), "RemoveAtSwapback did not properly remove from an element");
            });

            array.RemoveAtSwapback(array.Count - 1);
            Assert.Multiple(() => {
                Assert.That(array, Has.Count.EqualTo(8), "Swapback failed at the final element.");
                Assert.That(array[^1], Is.EqualTo(8), "The last element should be 7.");
            });
        }
    }
}
