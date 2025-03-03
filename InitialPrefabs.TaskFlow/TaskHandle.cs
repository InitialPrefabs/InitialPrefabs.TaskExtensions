using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow {

    public interface INode<T> where T : unmanaged {
        T LocalID { get; }
        T GlobalID { get; }
        ReadOnlySpan<T> GetDependencies();
        bool IsEmpty();
    }

    public static class TaskHandleExtensions {
        private static ushort UniqueID;
        internal static readonly TaskGraph TaskGraph;

        static TaskHandleExtensions() {
            UniqueID = 0;
            TaskGraph = new TaskGraph(10);
        }

        public static void Reset() {
            UniqueID = 0;
        }

        public static ushort GetUniqueID() {
            return UniqueID++;
        }

        public static TaskHandle<T0> Schedule<T0>(this T0 task) where T0 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent();
            ref var metadata = ref TaskUnitPool<T0>.ElementAt(handle);
            metadata.Reset();
            metadata.Task = task;
            metadata.Workload = new Workload {
                ThreadCount = 1,
                WorkDonePerThread = 0
            };

            var taskHandle = new TaskHandle<T0>(handle);
            TaskGraph.Track(taskHandle);
            return taskHandle;
        }

        public static TaskHandle<T0> Schedule<T0, T1>(this T0 task, TaskHandle<T1> dependsOn)
            where T0 : struct, ITaskFor
            where T1 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent();
            ref var metadata = ref TaskUnitPool<T0>.ElementAt(handle);
            metadata.Reset();
            metadata.Task = task;
            metadata.Workload = new Workload {
                ThreadCount = 1,
                WorkDonePerThread = 0
            };

            // TODO: Track the task handle (will have to do boxing)
            var taskHandle = new TaskHandle<T0>(handle) {
                Parents = new FixedUInt16Array32 {
                    dependsOn.GlobalID
                }
            };

            TaskGraph.Track(taskHandle);
            return taskHandle;
        }

        public static TaskHandle<T0> Schedule<T0>(this T0 task, Span<INode<ushort>> dependsOn)
            where T0 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent();
            ref var metadata = ref TaskUnitPool<T0>.ElementAt(handle);

            metadata.Reset();
            metadata.Task = task;
            metadata.Workload = new Workload {
                ThreadCount = 1,
                WorkDonePerThread = 0
            };

            var dependencies = new FixedUInt16Array32();
            for (var i = 0; i < dependsOn.Length; i++) {
                // TODO: Fix how we depend on a handle, we need a global id not a local id
                dependencies.Add(dependsOn[i].GlobalID);
            }

            // TODO: Track the task handle (will have to do boxing)
            var taskHandle =new TaskHandle<T0>(handle) {
                Parents = dependencies
            };
            TaskGraph.Track(taskHandle);
            return taskHandle;
        }

        // TODO: Add a way to combine one handle with another
    }

    public struct TaskHandle<T0> : INode<ushort>
        where T0 : struct, ITaskFor {

        internal readonly LocalHandle<T0> LocalHandle;
        internal readonly ushort GlobalHandle;

        // TODO: Add the dependencies, the dependencies need to store the handles.
        internal FixedUInt16Array32 Parents;

        public TaskHandle(LocalHandle<T0> handle) {
            LocalHandle = handle;
            Parents = new FixedUInt16Array32();
            GlobalHandle = TaskHandleExtensions.GetUniqueID();
        }

        public readonly ushort LocalID => LocalHandle;

        public readonly ushort GlobalID => GlobalHandle;

        public readonly ReadOnlySpan<ushort> GetDependencies() {
            unsafe {
                fixed (byte* ptr = Parents.Data) {
                    var head = (ushort*)ptr;
                    return new ReadOnlySpan<ushort>(ptr, Parents.Count);
                }
            }
        }

        public readonly bool IsEmpty() {
            return Parents.Count == 0;
        }
    }
}

