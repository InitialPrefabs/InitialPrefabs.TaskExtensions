using Silk.NET.OpenGL;

namespace InitialPrefabs.TaskFlow.Examples {
    public readonly struct VertexArrayObject<TVertexType, TIndexType>
        where TVertexType : unmanaged
        where TIndexType : unmanaged {

        private readonly uint handle;
        private readonly GL gl;

        public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo) {
            this.gl = gl;
            handle = gl.GenVertexArray();

        }

        public readonly VertexArrayObject<TVertexType, TIndexType> VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet) {
            unsafe {
                gl.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(TVertexType), (void*)(offSet * sizeof(TVertexType)));
                gl.EnableVertexAttribArray(index);
            }
            return this;
        }

        public void Bind() {
            gl.BindVertexArray(handle);
        }

        public void Dispose() {
            gl.DeleteVertexArray(handle);
        }
    }
}
