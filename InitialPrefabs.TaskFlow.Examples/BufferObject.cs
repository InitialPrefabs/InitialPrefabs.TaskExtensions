using Silk.NET.OpenGL;
using System;

namespace InitialPrefabs.TaskFlow.Examples {

    public readonly struct BufferObject<T> : IDisposable where T : unmanaged {

        private readonly uint handle;
        private readonly BufferTargetARB bufferType;
        private readonly GL gl;

        public readonly uint Length;

        public BufferObject(GL gl, Span<T> data, BufferTargetARB bufferType) {
            this.gl = gl;
            this.bufferType = bufferType;
            handle = gl.GenBuffer();
            Bind();
            Length = (uint)data.Length;

            unsafe {
                fixed (void* ptr = data) {
                    gl.BufferData(
                        bufferType,
                        InteropUtils.SizeOf(data),
                        ptr,
                        BufferUsageARB.StaticDraw);
                }
            }
        }

        public readonly void Bind() {
            gl.BindBuffer(bufferType, handle);
        }

        public readonly void Dispose() {
            gl.DeleteBuffer(handle);
        }
    }
}

