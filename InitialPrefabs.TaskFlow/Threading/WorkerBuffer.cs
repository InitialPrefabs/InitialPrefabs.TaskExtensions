using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow.Threading {

    public sealed class WorkerBuffer : IDisposable {

        internal unsafe struct _Buffer {
            public fixed byte Data[TaskConstants.MaxTasks];

            public readonly Span<WorkerHandle> AsSpan(int capacity) {
                fixed (void* ptr = Data) {
                    var head = (WorkerHandle*)ptr;
                    return new Span<WorkerHandle>(head, capacity);
                }
            }

            public readonly Span<WorkerHandle> AsSpan() {
                fixed (void* ptr = Data) {
                    var head = (WorkerHandle*)ptr;
                    return new Span<WorkerHandle>(head, TaskConstants.MaxTasks);
                }
            }

            public byte this[int i] => Data[i];
        }

        internal readonly TaskWorker[] Workers;
        internal _Buffer Free;
        internal _Buffer Used;

        internal ushort FreeCounter;
        internal ushort UseCounter;

        public WorkerBuffer() {
            Workers = new TaskWorker[TaskConstants.MaxTasks];
            var free = new NoAllocList<WorkerHandle>(
                Free.AsSpan(TaskConstants.MaxTasks),
                FreeCounter);

            for (var i = 0; i < TaskConstants.MaxTasks; i++) {
                Workers[i] = new TaskWorker();
                free.Add(new WorkerHandle((byte)i));
            }
            FreeCounter = TaskConstants.MaxTasks;
        }

        ~WorkerBuffer() {
            Dispose();
        }

        public (WorkerHandle handle, TaskWorker worker) Rent() {
            var free = new NoAllocList<WorkerHandle>(
                Free.AsSpan(),
                FreeCounter);

            if (free.Count > 0) {
                var element = free[0];
                free.RemoveAtSwapback(0);
                FreeCounter = (ushort)free.Count;

                var use = new NoAllocList<WorkerHandle>(
                    Used.AsSpan(),
                    UseCounter);
                use.Add(element);
                UseCounter = (ushort)use.Count;

                return (element, Workers[element]);
            }
            throw new InvalidOperationException("Cannot rent a Worker due to limited capacity.");
        }

        public void Return((WorkerHandle handle, TaskWorker worker) value) {
            var free = new NoAllocList<WorkerHandle>(
                Free.AsSpan(),
                FreeCounter);
            free.Add(value.handle);
            _ = value.worker.Reset();
            FreeCounter = (ushort)free.Count;

            var use = new NoAllocList<WorkerHandle>(
                Used.AsSpan(), UseCounter);

            var idx = use.IndexOf(value.handle);
            use.RemoveAtSwapback(idx);
            UseCounter = (ushort)use.Count;
        }

        public void Dispose() {
            for (var i = 0; i < Workers.Length; i++) {
                Workers[i].Dispose();
            }
        }
    }
}

