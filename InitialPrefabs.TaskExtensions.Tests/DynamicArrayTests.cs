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
    }
}
