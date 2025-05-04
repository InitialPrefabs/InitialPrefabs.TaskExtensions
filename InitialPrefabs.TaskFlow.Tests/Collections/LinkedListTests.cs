using NUnit.Framework;

namespace InitialPrefabs.TaskFlow.Collections.Tests {

    public class LinkedListTests {

        private LinkedList<int> linkedList;

        [SetUp]
        public void SetUp() {
            linkedList = new LinkedList<int>(5);
            Assert.Multiple(() => {
                Assert.That(linkedList.HeadIndex, Is.EqualTo(-1));
                Assert.That(linkedList.TailIndex, Is.EqualTo(-1));
            });
        }

        private void CommonFirstTest(short head, short tail, short expected) {
            var first = linkedList.Nodes[0];
            Assert.Multiple(() => {
                Assert.That(linkedList.Nodes[0], Is.Not.EqualTo(default));
                Assert.That(first, Is.EqualTo(new LinkedList<int>.Node<int> {
                    NextIdx = -1,
                    PreviousIdx = -1,
                    Value = expected
                }));

                Assert.That(
                    linkedList,
                    Has.Count.EqualTo(1),
                    "Nothing added to linked list.");

                Assert.That(linkedList.HeadIndex, Is.EqualTo(head));
                Assert.That(linkedList.TailIndex, Is.EqualTo(tail));
            });
        }

        [Test]
        public void AddToHeadTest() {
            var value = linkedList.AddFirst(10);
            Assert.That(value, Is.EqualTo(10));
            CommonFirstTest(0, 0, 10);
        }

        [Test]
        public void AppendTest() {
            var value = linkedList.Append(11);
            Assert.That(value, Is.EqualTo(11));
            CommonFirstTest(0, 0, 11);
            value = linkedList.Append(12);
            Assert.That(value, Is.EqualTo(12));
            Assert.Multiple(() => {
                Assert.That(linkedList, Has.Count.EqualTo(2),
                    "Tail not added.");
                Assert.That(linkedList.TailIndex, Is.EqualTo(4),
                    "The tail should have been updated.");
            });
        }

        private void CommonMiddleTest() {
            Assert.Multiple(() => {
                Assert.That(linkedList.Append(1), Is.EqualTo(1));
                Assert.That(linkedList.Append(2), Is.EqualTo(2));
                Assert.That(linkedList.Append(3), Is.EqualTo(3));
            });

            var i = 0;
            foreach (var node in linkedList) {
                Assert.Multiple(() => {
                    Assert.That(node.Value, Is.EqualTo(i + 1));
                    switch (i) {
                        case 0:
                            Assert.That(node.PreviousIdx, Is.EqualTo(-1));
                            Assert.That(node.NextIdx, Is.EqualTo(4));
                            break;
                        case 1:
                            Assert.That(node.PreviousIdx, Is.EqualTo(0));
                            Assert.That(node.NextIdx, Is.EqualTo(3));
                            break;
                        case 2:
                            Assert.That(node.PreviousIdx, Is.EqualTo(4));
                            Assert.That(node.NextIdx, Is.EqualTo(-1));
                            break;
                        default:
                            break;
                    }
                });
                i++;
            }
        }

        [Test]
        public void RemoveInMiddleTest() {
            CommonMiddleTest();
            linkedList.Remove(2);
            Assert.That(linkedList, Has.Count.EqualTo(2));
            Assert.Multiple(() => {
                Assert.That(linkedList.Head.NextIdx,
                    Is.EqualTo(linkedList.UseList[^1]));
                Assert.That(linkedList.Head.PreviousIdx, Is.EqualTo(-1));
                Assert.That(linkedList.Head.Value, Is.EqualTo(1));

                Assert.That(linkedList.Tail.NextIdx, Is.EqualTo(-1));
                Assert.That(linkedList.Tail.PreviousIdx,
                    Is.EqualTo(linkedList.UseList[0]));
                Assert.That(linkedList.Tail.Value, Is.EqualTo(3));
            });
        }

        [Test]
        public void RemoveHeadTest() {
            CommonMiddleTest();
            linkedList.Remove(1);
            Assert.That(linkedList, Has.Count.EqualTo(2));
            Assert.Multiple(() => {
                Assert.That(linkedList.Head.Value, Is.EqualTo(2));
                Assert.That(linkedList.Head.PreviousIdx, Is.EqualTo(-1));
                Assert.That(linkedList.Head.NextIdx, Is.EqualTo(3));
            });
        }

        [Test]
        public void RemoveTailTest() {
            CommonMiddleTest();
            linkedList.Remove(3);
            Assert.That(linkedList, Has.Count.EqualTo(2));
            Assert.Multiple(() => {
                Assert.That(linkedList.Tail.Value, Is.EqualTo(2));
                Assert.That(linkedList.Tail.PreviousIdx, Is.EqualTo(0));
                Assert.That(linkedList.Tail.NextIdx, Is.EqualTo(-1));
            });
        }

        [Test]
        public void ClearTest() {
            CommonMiddleTest();
            linkedList.Clear();
            Assert.That(linkedList, Has.Count.EqualTo(0));
        }
    }
}

