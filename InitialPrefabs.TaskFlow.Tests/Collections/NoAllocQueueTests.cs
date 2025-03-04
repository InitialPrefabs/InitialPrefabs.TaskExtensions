using NUnit.Framework;
using System;

namespace InitialPrefabs.TaskFlow.Collections.Tests {

    public class NoAllocQueueTests {

        [Test]
        public void EnqueueTest() {
            Assert.Multiple(static () => {
                Span<int> _q = stackalloc int[10];
                var queue = new NoAllocQueue<int>(_q);
                Assert.That(queue.TryEnqueue(1), "Failed to insert a single element.");
                Assert.That(queue.Count, Is.EqualTo(1), "Did not enqueue");
                Assert.That(queue.TryPeek(out var item), "Peek was not successful");
                Assert.That(item, Is.EqualTo(1),
                        "Did not enqueue the value 1");

                var element = queue.Dequeue();
                Assert.That(element, Is.EqualTo(item),
                        "Dequeing element from queue was not successful.");
                Assert.That(queue.IsEmpty, "Failed to remove from the queue.");

                for (var i = 0; i < 10; i++) {
                    Assert.That(queue.TryEnqueue(i + 10),
                            $"Failed to enqueue value: {i + 10} in a loop");
                }

                Assert.That(queue.Count, Is.EqualTo(10),
                        "Did not successfully enqueue.");
                queue.Clear();
                Assert.That(queue.Count, Is.EqualTo(0),
                        "Did not clear the queue.");
            });
        }
    }
}


