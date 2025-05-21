using NUnit.Framework;
using System;

namespace InitialPrefabs.TaskFlow.Collections.Tests {

    public class NoAllocListTests {

        [Test]
        public void AddingToList() {
            Span<int> _list = stackalloc int[10];
            var list = new NoAllocList<int>(_list);

            Assert.That(list.Count, Is.EqualTo(0),
                "No elements should be stored into the span...");

            list.Add(0);
            var actual = list.Count;
            var first = list[0];

            Assert.Multiple(() => {
                Assert.That(
                    actual,
                    Is.EqualTo(1),
                    "Element should have been added.");

                Assert.That(
                    first,
                    Is.EqualTo(0),
                    "Element should have been added.");
            });

            for (var i = 0; i < list.Capacity - 1; i++) {
                list.Add(i + 1);
            }

            for (var i = 0; i < list.Capacity; i++) {
                Assert.That(
                    list[i],
                    Is.EqualTo(i),
                    "Did not successfully add...");
            }
        }

        [Test]
        public void RemoveSwapbackFromList() {
            Span<int> _list = stackalloc int[10];
            var list = new NoAllocList<int>(_list);
            for (var i = 0; i < 5; i++) {
                list.Add(i);
                var actual = list[i];
                var count = list.Count;

                Assert.Multiple(() => {
                    Assert.That(actual, Is.EqualTo(i));
                    Assert.That(count, Is.EqualTo(i + 1));
                });
            }

            list.RemoveAtSwapback(0);
            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list[0], Is.EqualTo(4));
        }

        [Test]
        public void RemoveFromList() {
            Span<int> _list = stackalloc int[10];
            var list = new NoAllocList<int>(_list);
            for (var i = 0; i < 5; i++) {
                list.Add(i);
            }

            list.RemoveAt(0);
            for (var i = 0; i < list.Count; i++) {
                Assert.That(list[i], Is.EqualTo(i + 1));
            }
        }
    }
}
