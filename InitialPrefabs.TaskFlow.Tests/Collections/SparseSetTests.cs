using NUnit.Framework;

namespace InitialPrefabs.TaskFlow.Collections.Tests {

    public class FixedPoolTests {

        struct S {
            public int Value;
        }

        private FixedPool<S> pool;

        [SetUp]
        public void Setup() {
            pool = new FixedPool<S>(5, new S());
            Assert.That(pool.RemainingFreeCount, Is.EqualTo(5));
        }

        [Test]
        public void FixedPoolRenting() {
            for (var i = 0; i < 5; i++) {
                Assert.That(pool.TryRent(out var rented), "Renting failed");
                Assert.That(pool.RemainingFreeCount, Is.EqualTo(5 - (i + 1)), "Failed to remove the freeIndex");
            }
        }
    }
}
