using InitialPrefabs.TaskFlow.Collections;
using System;

namespace InitialPrefabs.TaskFlow {

    public interface INode<T> where T : unmanaged {
        T ID();
        ReadOnlySpan<T> Children();
        bool IsEmpty();
    }

    public struct TaskHandle<T0> : INode<ushort>
        where T0 : struct, ITaskFor {
        internal readonly Handle<T0> Handle;

        // TODO: Add the dependencies, the dependencies need to store the handles.
        internal FixedUInt16Array32 Dependencies;

        public TaskHandle(Handle<T0> handle) {
            Handle = handle;
            Dependencies = new FixedUInt16Array32();
        }

        public readonly ReadOnlySpan<ushort> Children() {
            unsafe {
                fixed (byte* ptr = Dependencies.Data) {
                    var head = (ushort*)ptr;
                    return new ReadOnlySpan<ushort>(ptr, Dependencies.Count);
                }
            }
        }

        public readonly bool IsEmpty() {
            return Dependencies.Count == 0;
        }

        public readonly ushort ID() {
            return Handle;
        }
    }
}

