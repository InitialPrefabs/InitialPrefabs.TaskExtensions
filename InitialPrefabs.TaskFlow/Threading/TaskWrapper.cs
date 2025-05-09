using InitialPrefabs.TaskFlow.Collections;
using InitialPrefabs.TaskFlow.Utils;
using System;

namespace InitialPrefabs.TaskFlow.Threading {

    internal class ExecutionContext {
        public ITaskFor Task;
        public int Length;
        public int Offset;

        public readonly Action TaskHandler;

        public ExecutionContext() {
            TaskHandler = () => {
                for (var i = 0; i < Length; i++) {
                    Task.Execute(i + Offset);
                }
            };
        }
    }

    internal static class ExecutionContextBuffer {

        internal static readonly DynamicArray<ExecutionContext> Contexts =
            new DynamicArray<ExecutionContext>(TaskConstants.MaxTasks);
        internal static readonly DynamicArray<ushort> FreeHandles =
            new DynamicArray<ushort>(TaskConstants.MaxTasks);
        internal static readonly DynamicArray<ushort> UseHandles =
            new DynamicArray<ushort>(TaskConstants.MaxTasks);

        static ExecutionContextBuffer() {
            Contexts.Clear();
            FreeHandles.Clear();
            UseHandles.Clear();

            for (ushort i = 0; i < TaskConstants.MaxTasks; i++) {
                Contexts.Add(new ExecutionContext { });
                FreeHandles.Add(i);
            }
        }

        public static (ExecutionContext ctx, ushort localHandle) Rent(
            ITaskFor task,
            int offset,
            int length) {

            var free = FreeHandles[0];
            FreeHandles.RemoveAtSwapback(0);
            UseHandles.Add(free);
            var context = Contexts.Collection[free];
            context.Task = task;
            context.Offset = offset;
            context.Length = length;
            return (context, free);
        }

        public static void Return(ushort localHandle) {
            var idx = UseHandles.IndexOf(localHandle, default(UShortComparer));
            if (idx > -1) {
                UseHandles.RemoveAtSwapback(idx);
                FreeHandles.Add(localHandle);
            }
        }
    }
}

