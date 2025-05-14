using InitialPrefabs.TaskFlow.Collections;
using System;
using System.Collections.Generic;

namespace InitialPrefabs.TaskFlow.Threading {

    internal readonly struct NodeMetadataComparer : IComparer<NodeMetadata> {
        public readonly int Compare(NodeMetadata x, NodeMetadata y) {
            return x.GlobalID.CompareTo(y.GlobalID);
        }
    }

    public struct NodeMetadata {
        internal ushort LocalID;
        internal ushort GlobalID;
        internal FixedUInt16Array32 Parents;

        public readonly bool IsEmpty() {
            return Parents.Count == 0;
        }
    }

    public interface INode<T> : IDisposable where T : unmanaged {
        ITaskUnitRef TaskRef { get; }
        NodeMetadata Metadata { get; }
    }

    public struct TaskHandle : INode<ushort> {

        public readonly NodeMetadata Metadata => new NodeMetadata {
            GlobalID = GlobalHandle,
            LocalID = LocalHandle,
            Parents = Parents
        };

        internal readonly LocalHandle LocalHandle;
        internal readonly ushort GlobalHandle;
        internal readonly ITaskDefPoolable Pool;

        // TODO: Add the dependencies, the dependencies need to store the handles.
        internal FixedUInt16Array32 Parents;

        internal static TaskHandle Get<T0>(LocalHandle local) where T0 : struct, ITaskFor {
            return new TaskHandle(local, TaskUnitPool<T0>.Pool);
        }

        private TaskHandle(LocalHandle local, ITaskDefPoolable pool) {
            LocalHandle = local;
            Pool = pool;
            Parents = new FixedUInt16Array32();
            GlobalHandle = TaskGraphRunner.UniqueID++;
        }

        public readonly ushort LocalID => LocalHandle;

        public readonly ushort GlobalID => GlobalHandle;

        public readonly ITaskUnitRef TaskRef => Pool.ElementAt(LocalHandle);

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
            Pool.ReturnHandle(LocalHandle);
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
        public static void DependsOn(this ref TaskHandle handle, TaskHandle dependsOn) {
            // TODO: This performs 2 copies
            var metadata = handle.Metadata;
            var handleIdx = TaskGraphRunner.Default.NodeMetadata.IndexOf(metadata,
                new NodeMetadataComparer());
            if (handleIdx > -1) {
                handle.Parents.Add(dependsOn.GlobalID);

                // TODO: This performs the 2nd copy because we updated the structs...
                TaskGraphRunner.Default.NodeMetadata[handleIdx] = handle.Metadata;
            } else {
                throw new InvalidOperationException($"TaskHandle {handle.GetType()} with " +
                    $"ID: {handle.GlobalID}, cannot track TaskHandle, {dependsOn.GetType()} " +
                    $"with Global ID: {dependsOn.GlobalID} because it is NOT properly tracked in a Graph.");
            }
        }

        public static TaskHandle Schedule<T0>(this T0 task) where T0 : struct, ITaskFor {
            var dependencies = Span<ushort>.Empty;
            return task.Schedule(dependencies);
        }

        public static TaskHandle Schedule<T0>(this T0 task, TaskHandle dependsOn)
            where T0 : struct, ITaskFor {
            Span<ushort> dependencies = stackalloc ushort[1] { dependsOn.GlobalID };
            return task.Schedule(dependencies);
        }

        public static TaskHandle Schedule<T0>(this T0 task, Span<ushort> dependsOn)
            where T0 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent(task);
            var dependencies = new FixedUInt16Array32();
            for (var i = 0; i < dependsOn.Length; i++) {
                dependencies.Add(dependsOn[i]);
            }

            var taskHandle = TaskHandle.Get<T0>(handle);
            taskHandle.Parents = dependencies;

            TaskGraphRunner.Default.Track(taskHandle, TaskWorkload.SingleUnit());
            return taskHandle;
        }

        public static TaskHandle Schedule<T0>(this T0 task, int length)
            where T0 : struct, ITaskFor {
            var dependencies = Span<ushort>.Empty;
            return task.Schedule(length, dependencies);
        }

        public static TaskHandle Schedule<T0>(this T0 task, int length, TaskHandle dependsOn)
            where T0 : struct, ITaskFor {
            Span<ushort> dependencies = stackalloc ushort[1] { dependsOn.GlobalID };
            return task.Schedule(length, dependencies);
        }

        public static TaskHandle Schedule<T0>(
            this T0 task,
            int length,
            Span<ushort> dependsOn)
            where T0 : struct, ITaskFor {

            var handle = TaskUnitPool<T0>.Rent(task);
            var parents = new FixedUInt16Array32();

            for (var i = 0; i < dependsOn.Length; i++) {
                parents.Add(dependsOn[i]);
            }

            var taskHandle = TaskHandle.Get<T0>(handle);
            taskHandle.Parents = parents;

            TaskGraphRunner.Default.Track(
                taskHandle,
                TaskWorkload.LoopedSingleUnit(length));
            return taskHandle;
        }

        public static TaskHandle ScheduleParallel<T0>(
            this T0 task,
            int total,
            int batchSize)
            where T0 : struct, ITaskFor {
            var dependencies = Span<ushort>.Empty;
            return task.ScheduleParallel(
                TaskWorkload.MultiUnit(total, batchSize),
                dependencies);
        }

        public static TaskHandle ScheduleParallel<T0>(
            this T0 task,
            int total,
            int batchSize,
            TaskHandle dependsOn)
            where T0 : struct, ITaskFor {
            Span<ushort> dependencies = stackalloc ushort[1] { dependsOn.GlobalID };
            return task.ScheduleParallel(
                TaskWorkload.MultiUnit(total, batchSize),
                dependencies);
        }

        public static TaskHandle ScheduleParallel<T0>(
            this T0 task,
            int total,
            int batchSize,
            Span<ushort> dependsOn)
            where T0 : struct, ITaskFor {

            return task.ScheduleParallel(
                TaskWorkload.MultiUnit(total, batchSize),
                dependsOn);
        }

        public static TaskHandle ScheduleParallel<T0>(
            this T0 task,
            TaskWorkload workload,
            Span<ushort> dependsOn)
            where T0 : struct, ITaskFor {
            var handle = TaskUnitPool<T0>.Rent(task);
            var parents = new FixedUInt16Array32();

            for (var i = 0; i < dependsOn.Length; i++) {
                parents.Add(dependsOn[i]);
            }

            var taskHandle = TaskHandle.Get<T0>(handle);
            taskHandle.Parents = parents;

            TaskGraphRunner.Default.Track(taskHandle, workload);
            return taskHandle;
        }

        // TODO: Add a way to combine one handle with another
    }
}

