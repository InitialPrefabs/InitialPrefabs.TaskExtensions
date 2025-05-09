using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    /// <summary>
    /// A cancellation token allowing you to cancel any threads if another thread fails.
    /// </summary>
    public struct AtomicCancellationToken {
        private int state;

        public readonly bool IsCancellationRequested => state > 0;

        public void Cancel() {
            _ = Interlocked.Exchange(ref state, 1);
        }

        public void Reset() {
            _ = Interlocked.Exchange(ref state, 0);
        }
    }
}

