using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Collections.Generic;

namespace InitialPrefabs.TaskFlow.Threading {

    public interface INode<T> : IDisposable where T : unmanaged {
        ushort LocalID { get; }
        ushort GlobalID { get; }
        ITaskFor Task { get; }
        ReadOnlySpan<T> GetDependencies();
        bool IsEmpty();
    }

    internal struct INodeComparer<T> : IComparer<INode<T>> where T : unmanaged {
        public readonly int Compare(INode<T> x, INode<T> y) {
            return x.GlobalID.CompareTo(y.GlobalID);
        }
    }

    public struct TaskHandle<T0> : INode<ushort>
        where T0 : struct, ITaskFor {

        internal readonly LocalHandle<T0> LocalHandle;
        internal readonly ushort GlobalHandle;

        // TODO: Add the dependencies, the dependencies need to store the handles.
        internal FixedUInt16Array32 Parents;

        public TaskHandle(LocalHandle<T0> local, GraphHandle graphHandle) {
            (var graph, var uniqueId) = TaskGraphManager.Get(graphHandle);
            LocalHandle = local;
            Parents = new FixedUInt16Array32();
            GlobalHandle = uniqueId.Ref++;
        }

        public TaskHandle(LocalHandle<T0> handle) : this(handle, default) { }

        public readonly ushort LocalID => LocalHandle;

        public readonly ushort GlobalID => GlobalHandle;

        public readonly ITaskFor Task => TaskUnitPool<T0>.ElementAt(LocalHandle);

        /// <summary>
        /// Allows the TaskHandle to be returned back to its associated
        /// <see cref="TaskUnitPool{T0}"/>.
        ///
        /// <remarks>
        /// Do not manually call this method! The <see cref="TaskGraph"/> will return
        /// all <see cref="INode{T}"/> to their <see cref="TaskUnitPool{T0}"/>.
        /// </remarks>
        /// </summary>
        public readonly void Dispose() {
            TaskUnitPool<T0>.Return(LocalHandle);
        }

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

    public static class TaskHandleExtensions {
        [Obsolete("Use the TaskGraphManager instead.", true)]
        internal const int Capacity = 10;

        [Obsolete("Use the TaskGraphManager instead.", true)]
        // TODO: Change this to byte if I am only supporting up to 256 tasks?
        private static ushort UniqueID;

        [Obsolete("Use the TaskGraphManager instead.", true)]
        // TODO: Move this to a public API or a TaskGraphManager
        internal static TaskGraph Graph;

        public static void DependsOn<T0, T1>(this ref TaskHandle<T0> handle, TaskHandle<T1> dependsOn, GraphHandle graphHandle = default)
            where T0 : struct, ITaskFor
            where T1 : struct, ITaskFor {

        var data = TaskGraphManager.Get(graphHandle);

        var handleIdx = data.graph.Nodes.IndexOf(handle, new INodeComparer<ushort>());
            if (handleIdx > -1) {
                handle.Parents.Add(dependsOn.GlobalID);
                data.graph.Nodes[handleIdx] = handle;
            } else {
                throw new InvalidOperationException($"TaskHandle {handle.GetType()} with " +
                    $"ID: {handle.GlobalID}, cannot track TaskHandle, {dependsOn.GetType()} " +
                    $"with Global ID: {dependsOn.GlobalID} because it is NOT properly tracked in a Graph.");
            }
        }

        public static TaskHandle<T0> Schedule<T0>(this T0 task) where T0 : struct, ITaskFor {
            var dependencies = Span<ushort>.Empty;
            return task.Schedule(dependencies);
        }

        public static TaskHandle<T0> Schedule<T0, T1>(this T0 task, TaskHandle<T1> dependsOn)
            where T0 : struct, ITaskFor
            where T1 : struct, ITaskFor {

            Span<ushort> dependencies = stackalloc ushort[1] { dependsOn.GlobalID };
            return task.Schedule(dependencies);
        }

        public static TaskHandle<T0> Schedule<T0>(this T0 task, Span<ushort> dependsOn, GraphHandle graphHandle = default)
            where T0 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent(task);
            var dependencies = new FixedUInt16Array32();
            for (var i = 0; i < dependsOn.Length; i++) {
                dependencies.Add(dependsOn[i]);
            }

            var taskHandle = new TaskHandle<T0>(handle) {
                Parents = dependencies
            };

            var pair = TaskGraphManager.Get(graphHandle);
            pair.graph.Track(taskHandle, TaskWorkload.SingleUnit());
            return taskHandle;
        }

        public static TaskHandle<T0> Schedule<T0>(this T0 task, int length)
            where T0 : struct, ITaskFor {
            var dependencies = Span<ushort>.Empty;
            return task.Schedule(length, dependencies);
        }

        public static TaskHandle<T0> Schedule<T0, T1>(this T0 task, int length, TaskHandle<T1> dependsOn)
            where T0 : struct, ITaskFor
            where T1 : struct, ITaskFor {
            Span<ushort> dependencies = stackalloc ushort[1] { dependsOn.GlobalID };
            return task.Schedule(length, dependencies);
        }

        public static TaskHandle<T0> Schedule<T0>(this T0 task, int length, Span<ushort> dependsOn)
            where T0 : struct, ITaskFor {

            var handle = TaskUnitPool<T0>.Rent(task);
            var parents = new FixedUInt16Array32();

            for (var i = 0; i < dependsOn.Length; i++) {
                parents.Add(dependsOn[i]);
            }

            var taskHandle = new TaskHandle<T0>(handle) {
                Parents = parents
            };

            Graph.Track(taskHandle, TaskWorkload.LoopedSingleUnit(length));
            return taskHandle;
        }

        public static TaskHandle<T0> ScheduleParallel<T0>(this T0 task, int total, int batchSize)
            where T0 : struct, ITaskFor {
            var dependencies = Span<ushort>.Empty;
            return task.ScheduleParallel(
                TaskWorkload.MultiUnit(total, batchSize), dependencies);
        }

        public static TaskHandle<T0> ScheduleParallel<T0, T1>(
            this T0 task,
            int total,
            int batchSize,
            TaskHandle<T1> dependsOn)
            where T0 : struct, ITaskFor
            where T1 : struct, ITaskFor {
            Span<ushort> dependencies = stackalloc ushort[1] { dependsOn.GlobalID };
            return task.ScheduleParallel(
                TaskWorkload.MultiUnit(total, batchSize),
                dependencies);
        }

        public static TaskHandle<T0> ScheduleParallel<T0>(
            this T0 task,
            int total,
            int batchSize,
            Span<ushort> dependsOn)
            where T0 : struct, ITaskFor {

            return task.ScheduleParallel(
                TaskWorkload.MultiUnit(total, batchSize),
                dependsOn);
        }

        public static TaskHandle<T0> ScheduleParallel<T0>(
            this T0 task,
            TaskWorkload workload,
            Span<ushort> dependsOn)
            where T0 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent(task);
            var parents = new FixedUInt16Array32();

            for (var i = 0; i < dependsOn.Length; i++) {
                parents.Add(dependsOn[i]);
            }

            var taskHandle = new TaskHandle<T0>(handle) {
                Parents = parents
            };

            Console.WriteLine($"From Scheduling: {workload}");
            Graph.Track(taskHandle, workload);
            return taskHandle;
        }

        // TODO: Add a way to combine one handle with another
    }
}

