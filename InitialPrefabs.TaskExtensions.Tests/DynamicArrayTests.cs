using NUnit.Framework;

namespace InitialPrefabs.TaskExtensions.Tests {
    public class DynamicArrayTests {

        [Test]
        public void ArrayInitialized() {
            var array = new DynamicArray<int>(10);
            Assert.That(array.Collection != null, "Array not intialized");
            Assert.That(array.Capacity == 10, "Array not initialized to the correct size");
            Assert.That(array.Count == 0, "Array is filled");
        }

        [Test]
        public void DynamicArrayFilled() {
            var array = new DynamicArray<int>(10);
            array.Push(1);

            Assert.That(array.Count == 1, "Value not added");
            Assert.That(array[array.Count - 1] == 1, "Value was not correctly stored");

            var span = array.AsReadOnlySpan();
            foreach (var e in span) {
                Assert.That(e == 1, "ReadOnlySpan was not correctly added");
            }
        }

        [Test]
        public void DynamicArrayFilledAndRemove() {
            var array = new DynamicArray<int>(10);
            for (var i = 0; i < 11; i++) {
                array.Push(i);
            }

            Assert.That(array.Capacity == 11, "Capacity was not resized during the push operation");
            array.Resize(20);
            Assert.That(array.Capacity == 20, "Resize operation was not valid");

            array.RemoveAt(0);
            Assert.That(array.Count == 10, "Value was not removed from the collection");
            Assert.That(array.Capacity == 20, "The capacity should not be adjusted.");

            Assert.That(array[0] == 1);
            array.RemoveAtSwapback(0);
            Assert.That(array[0] == 10, "The last element should be swapped with the first element and removed.");
            Assert.That(array.Count == 9, "RemoveAtSwapback did not properly remove from an element");

            array.RemoveAtSwapback(array.Count - 1);
            Assert.That(array.Count == 8, "Swapback failed at the final element.");
        }
    }
}
