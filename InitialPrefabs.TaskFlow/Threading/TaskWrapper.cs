using InitialPrefabs.TaskFlow.Collections;

namespace InitialPrefabs.TaskFlow.Threading {

    internal static class TaskWrapperBuffer {

        internal static readonly DynamicArray<TaskContext> TaskWrappers = new DynamicArray<TaskContext>(TaskConstants.MaxTasks);
        internal static readonly DynamicArray<ushort> FreeList = new DynamicArray<ushort>(TaskConstants.MaxTasks);
        internal static readonly DynamicArray<ushort> UseList = new DynamicArray<ushort>(TaskConstants.MaxTasks);

        static TaskWrapperBuffer() {
            TaskWrappers.Clear();
            FreeList.Clear();
            UseList.Clear();

            for (ushort i = 0; i < TaskConstants.MaxTasks; i++) {
                TaskWrappers.Add(new TaskContext { });
                FreeList.Add(i);
            }
        }

        public static (TaskContext wrapper, ushort localIndex) Rent(ITaskFor task, int offset, int length) {
            var free = FreeList[0];
            FreeList.RemoveAtSwapback(0);
            UseList.Add(free);
            var context = TaskWrappers.Collection[free];
            context.Task = task;
            context.Offset = offset;
            context.Length = length;
            return (context, free);
        }

        public static void Return(ushort localIndex) {
            var idx = UseList.Find(element => element == localIndex);
            if (idx > -1) {
                UseList.RemoveAtSwapback(idx);
                FreeList.Add(localIndex);
            }
        }
    }

    public class TaskContext {
        public ITaskFor Task;
        public int Length;
        public int Offset;

        public void ExecuteLoop() {
            for (var i = 0; i < Length; i++) {
                Task.Execute(i + Offset);
            }
        }

        public void ExecuteNoLoop() {
            Task.Execute(-1);
        }
    }
}

