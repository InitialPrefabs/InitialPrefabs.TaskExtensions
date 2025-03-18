using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace InitialPrefabs.TaskFlow.Threading {

    // The idea is that a UnitTask represents a single allocated
    // worker on a thread.
    public class UnitTask {

        public Exception Err { get; private set; }

        private ManualResetEvent waitHandle;
        private TaskState state;

        public UnitTask() {
            state = TaskState.NotStarted;
            waitHandle = new ManualResetEvent(false);
        }

        public void Start([NotNull] Action action) {
            state = TaskState.InProgress;
            _ = ThreadPool.UnsafeQueueUserWorkItem(_ => {
                try {
                    action.Invoke();
                } catch (Exception err) {
                    this.Err = err;
                } finally {
                    state = Err != null ? TaskState.Faulted : TaskState.Completed;
                    _ = waitHandle.Set();
                }
            }, null);
        }

        public void Reset() {
            state = TaskState.NotStarted;
            _ = waitHandle.Reset();
        }
    }

    public class AwaitableTaskHandleAwaiter : INotifyCompletion {
        public bool IsCompleted => handle.IsCompleted;

        private AwaitableTaskHandle handle;
        public Action continuation;

        public AwaitableTaskHandleAwaiter(AwaitableTaskHandle handle) {
            this.handle = handle;
        }

        public void Wait() {
            if (handle.IsFaulted) {
                throw handle.Err;
            }
        }

        public void GetResult() {
            if (handle.IsFaulted) {
                throw handle.Err;
            }
        }

        public void OnCompleted(Action continuation) {
            this.continuation = continuation;
            if (handle.IsCompleted) {
                continuation();
            } else {
                handle.Continuation = continuation;
            }
        }
    }

    public class AwaitableTaskHandle {
        public Action Continuation;
        private Action action;
        private ManualResetEvent waitHandle;
        internal Exception Err;

        public bool IsCompleted { get; private set; }
        public bool IsFaulted => Err != null;

        public AwaitableTaskHandle(Action action) {
            this.action = action;
            waitHandle = new ManualResetEvent(false);
        }

        public void Wait() {
            _ = waitHandle.WaitOne();
            if (IsFaulted) {
                throw Err;
            }
        }

        public void Complete() {
            IsCompleted = true;
            _ = waitHandle.Set();
            Continuation?.Invoke();
        }

        public void Start() {
            _ = ThreadPool.QueueUserWorkItem(callback => {
                try {
                    action?.Invoke();
                } catch (Exception err) {
                    this.Err = err;
                } finally {
                    Complete();
                }
            });
        }

        public AwaitableTaskHandleAwaiter GetAwaiter() => new AwaitableTaskHandleAwaiter(this);

        public static AwaitableTaskHandle Run(Action action) {
            var handle = new AwaitableTaskHandle(action);
            handle.Start();
            return handle;
        }
    }
}
