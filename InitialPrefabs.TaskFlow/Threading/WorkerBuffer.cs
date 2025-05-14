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

        internal readonly DynamicArray<TaskWorker> Workers;
        internal _Buffer Free;
        internal _Buffer Used;

        internal ushort FreeCounter;
        internal ushort UseCounter;

        public WorkerBuffer() : this(TaskConstants.MaxTasks) { }

        public WorkerBuffer(int capacity) {
            if (capacity > TaskConstants.MaxTasks) {
                throw new InvalidOperationException(
                    "Cannot allocate more than the total # of tasks supported (256)!");
            }
            Workers = new DynamicArray<TaskWorker>(capacity);
            var free = new NoAllocList<WorkerHandle>(
                Free.AsSpan(capacity),
                FreeCounter);

            for (var i = 0; i < capacity; i++) {
                Workers.Add(new TaskWorker());
                free.Add(new WorkerHandle((byte)i));
            }
            FreeCounter = (ushort)capacity;
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

        public void ReturnAll() {
            var free = new NoAllocList<WorkerHandle>(Free.AsSpan(), FreeCounter);
            var useHandles = new NoAllocList<WorkerHandle>(Used.AsSpan(), UseCounter);
            foreach (var handle in useHandles) {
                var worker = Workers[handle];
                _ = worker.Reset();
                free.Add(handle);
            }
            FreeCounter = (ushort)free.Count;
            useHandles.Clear();
            UseCounter = 0;
        }

        public void Dispose() {
            for (var i = 0; i < Workers.Capacity; i++) {
                Workers[i].Dispose();
            }
            Workers.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

