using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace InitialPrefabs.TaskFlow {
    public class TaskHandleAwaiter : INotifyCompletion {
        public bool IsCompleted => handle.IsCompleted;

        private AwaitableTaskHandle handle;
        public Action continuation;

        public TaskHandleAwaiter(AwaitableTaskHandle handle) {
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

        public TaskHandleAwaiter GetAwaiter() => new TaskHandleAwaiter(this);

        public static AwaitableTaskHandle Run(Action action) {
            var handle = new AwaitableTaskHandle(action);
            handle.Start();
            return handle;
        }
    }
}
