using NUnit.Framework;
using System;

namespace InitialPrefabs.TaskFlow.Collections.Tests {

    public class NoAllocBitArrayTests {

        [Test]
        public void BitArrayValidation() {
            Span<byte> bytes = stackalloc byte[4];
            var bitArray = new NoAllocBitArray(bytes);
            Assert.That(bitArray.Length,
                    Is.EqualTo(bytes.Length * 4),
                    "A byte stores 4 bits, so the the Length must be Length * 4");

            for (var i = 0; i < bitArray.Length; i++) {
                bitArray[i] = true;
            }

            for (var i = 0; i < bitArray.Length; i++) {
                Assert.That(bitArray[i], "All elements must be true.");
            }
        }

        [Test]
        public void CalculateBitArraySize() {
            Assert.Multiple(static () => {
                Assert.That(NoAllocBitArray.CalculateSize(4), Is.EqualTo(1),
                    "A bool is represented by 1 byte or 4 bits");
                Assert.That(NoAllocBitArray.CalculateSize(5), Is.EqualTo(2),
                    "To represent 5 bools, we need 2 bytes, or 8 bits in total");
            });
        }

        [Test]
        public void TogglingElementInBitArray() {
            Span<byte> bytes = stackalloc byte[4];
            var bitArray = new NoAllocBitArray(bytes);

            for (var i = 0; i < bitArray.Length; i++) {
                bitArray[i] = true;
            }

            bitArray[1] = false;
            for (var i = 0; i < bitArray.Length; i++) {
                Assert.That(bitArray[i], Is.EqualTo(i != 1),
                    "Did not toggle the 2nd element in the bitArray.");
            }
        }

        [Test]
        public void ResettingBitArrayEnumerator() {
            Span<byte> bytes = stackalloc byte[4];
            var bitArray = new NoAllocBitArray(bytes);

            for (var i = 0; i < bitArray.Length; i++) {
                bitArray[i] = true;
            }

            var it = new NoAllocBitArrayEnumerator(bitArray);
            while (it.MoveNext()) {
                Assert.That(it.Current, "Element was not set to true");
            }
            it.Reset();

            Assert.That(it.Index, Is.EqualTo(-1), "Did not reset the iterator to the head.");
        }
    }
}

