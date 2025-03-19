using InitialPrefabs.TaskFlow.Collections;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace InitialPrefabs.TaskFlow.Threading.Tests {

    public class AtomicCancellationTokenTests {

        [Test]
        public async Task ThreadACancelsThreadB() {
            var isPreempted = false;
            var atomic = new AtomicCancellationToken();
            var refToken = new UnmanagedRef<AtomicCancellationToken>(ref atomic);

            var a = Task.Factory.StartNew(() => {
                Thread.Sleep(100);
                refToken.Ref.Cancel();
            });

            var b = Task.Factory.StartNew(() => {
                Thread.Sleep(200);
                if (refToken.Ref.IsCancellationRequested) {
                    isPreempted = true;
                    return;
                }
                System.Console.WriteLine("This msg should not printed in console.");
            });

            await Task.WhenAll(a, b);

            Assert.Multiple(() => {
                Assert.That(isPreempted,
                    "Failed to exit early when ThreadA requested a cancellation on ThreadB");
                Assert.That(atomic.IsCancellationRequested, "Cancellation should've been requested");
            });
        }

        [Test]
        public void ResettingACancellationToken() {
            var atomic = new AtomicCancellationToken();
            atomic.Cancel();
            Assert.That(atomic.IsCancellationRequested, "Cancellation should've been requested");
            atomic.Reset();
            Assert.That(atomic.IsCancellationRequested, Is.Not.True,
                "Cancellation should've been requested");
        }
    }
}

