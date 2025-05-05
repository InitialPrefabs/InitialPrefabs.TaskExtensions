using InitialPrefabs.TaskFlow.Collections;

namespace InitialPrefabs.TaskFlow.Threading {

    internal static class TaskWrapperBuffer {

        internal static readonly DynamicArray<TaskWrapper> TaskWrappers = new DynamicArray<TaskWrapper>(TaskConstants.MaxTasks);
        internal static readonly DynamicArray<ushort> FreeList = new DynamicArray<ushort>(TaskConstants.MaxTasks);
        internal static readonly DynamicArray<ushort> UseList = new DynamicArray<ushort>(TaskConstants.MaxTasks);

        static TaskWrapperBuffer() {
            TaskWrappers.Clear();
            FreeList.Clear();
            UseList.Clear();

            for (ushort i = 0; i < TaskConstants.MaxTasks; i++) {
                TaskWrappers.Add(new TaskWrapper { });
                FreeList.Add(i);
            }
        }

        public static (TaskWrapper wrapper, ushort localIndex) Rent() {
            var free = FreeList[0];
            FreeList.RemoveAtSwapback(0);
            UseList.Add(free);
            return (TaskWrappers.Collection[free], free);
        }

        public static void Return(ushort localIndex) {
            var idx = UseList.Find(element => element == localIndex);
            if (idx > -1) {
                UseList.RemoveAtSwapback(idx);
                FreeList.Add(localIndex);
            }
        }
    }

    public class TaskWrapper {
        public ITaskFor Task;
        public int Length;
        public int Offset;

        public void ExecuteLoop() {
            for (var i = Offset; i < Length; i++) {
                Task.Execute(i);
            }
        }

        public void ExecuteNoLoop() {
            Task.Execute(-1);
        }
    }
}

