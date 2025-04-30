using NUnit.Framework;

namespace InitialPrefabs.TaskFlow.Collections.Tests {

    public class LinkedListTests {

        private LinkedList<int> linkedList;

        [SetUp]
        public void SetUp() {
            linkedList = new LinkedList<int>(5);
            Assert.Multiple(() => {
                Assert.That(linkedList.Head, Is.EqualTo(-1));
                Assert.That(linkedList.Tail, Is.EqualTo(-1));
            });
        }

        private void CommonFirstTest(short head, short tail, short expected) {
            var first = linkedList.Nodes[0];
            Assert.Multiple(() => {
                Assert.That(linkedList.Nodes[0], Is.Not.EqualTo(default));
                Assert.That(first, Is.EqualTo(new LinkedList<int>.Node<int> {
                    Next = 4,
                    Previous = -1,
                    Value = expected
                }));

                Assert.That(
                    linkedList,
                    Has.Count.EqualTo(1),
                    "Nothing added to linked list.");

                Assert.That(linkedList.Head, Is.EqualTo(head));
                Assert.That(linkedList.Tail, Is.EqualTo(tail));
            });
        }

        [Test]
        public void AddToHeadTest() {
            linkedList.AddFirst(10);
            CommonFirstTest(0, 0, 10);
        }

        [Test]
        public void AppendTest() {
            linkedList.Append(11);
            CommonFirstTest(0, 0, 11);
            linkedList.Append(12);
            Assert.Multiple(() => {
                Assert.That(linkedList, Has.Count.EqualTo(2),
                    "Tail not added.");
                Assert.That(linkedList.Tail, Is.EqualTo(4),
                    "The tail should have been updated.");
            });
        }
    }
}

