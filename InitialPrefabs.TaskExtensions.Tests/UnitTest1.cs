using NUnit.Framework;

namespace InitialPrefabs.TaskExtensions.Tests {

    public class Tests {
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void Test1() {
            Class1.Add(1, 1, out var c);
            Assert.That(c == 2);
        }
    }
}