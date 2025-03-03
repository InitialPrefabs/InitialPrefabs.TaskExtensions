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
    }
}

