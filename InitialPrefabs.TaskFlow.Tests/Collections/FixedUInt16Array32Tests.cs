using NUnit.Framework;

namespace InitialPrefabs.TaskFlow.Collections.Tests {
    public unsafe class FixedUInt16Array32Tests {

        [Test]
        public void AddTest() {
            var fixedArray = new FixedUInt16Array32();
            for (var i = FixedUInt16Array32.Capacity - 1; i >= 0; i--) {
                fixedArray.Add((ushort)i);
                var fillAmount = FixedUInt16Array32.Capacity - i;
                Assert.That(fixedArray,
                    Has.Count.EqualTo(fillAmount),
                    "Not added to the fixedArray.");
            }
        }

        [Test]
        public void RemoveAtTest() {
            var fixedArray = new FixedUInt16Array32();
            for (var i = 0; i < 10; i++) {
                fixedArray.Add((ushort)i);
            }

            Assert.Multiple(() => {
                Assert.That(fixedArray, Has.Count.EqualTo(10), "Failed to fill fixed array");
                for (var i = 0; i < fixedArray.Count; i++) {
                    Assert.That(fixedArray[i], Is.EqualTo(i), "Did not correctly add the value.");
                }
            });

            Assert.Multiple(() => {
                fixedArray.RemoveAt(1);
                Assert.That(fixedArray, Has.Count.EqualTo(9), "Failed to remove from the fixed array");
                var array = new ushort[] { 0, 2, 3, 4, 5, 6, 7, 8, 9 };

                for (var i = 0; i < array.Length; i++) {
                    Assert.That(fixedArray[i], Is.EqualTo(array[i]), "Did not correctly remove from the array");
                }
            });
        }

        [Test]
        public void RemoveAtSwapbackTest() {
            var fixedArray = new FixedUInt16Array32();
            for (var i = 0; i < 5; i++) {
                fixedArray.Add((ushort)i);
            }
            Assert.That(fixedArray, Has.Count.EqualTo(5), "Failed to add.");

            fixedArray.RemoveAtSwapback(2);

            Assert.Multiple(() => {
                var expected = new ushort[] { 0, 1, 4, 3 };
                Assert.That(fixedArray[2], Is.EqualTo(4), "Mismatched elements.");
                Assert.That(fixedArray, Has.Count.EqualTo(4), "Did not successfully remove from the fixedArray.");

                var idx = 0;
                foreach (var element in fixedArray) {
                    Assert.That(element, Is.EqualTo(expected[idx++]), "Enumerator failed to iterate.");
                }
            });
        }
    }
}
