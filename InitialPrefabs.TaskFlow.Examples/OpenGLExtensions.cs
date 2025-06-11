using Silk.NET.OpenGL;
using System;
using System.Runtime.InteropServices;

namespace InitialPrefabs.TaskFlow.Examples {

    public static class OpenGLExtensions {
        private const string Spirv = "GL_ARB_gl_spirv";

        public static uint CompileAndLoadShader(this GL gl, ShaderType shaderType, string shaderSrc) {
            var shaderId = gl.CreateShader(shaderType);
            gl.ShaderSource(shaderId, shaderSrc);
            gl.CompileShader(shaderId);
            gl.GetShader(shaderId, ShaderParameterName.CompileStatus, out var status);

            return status != (int)GLEnum.True ?
                throw new InvalidOperationException($"Failed to compile: {shaderSrc}") :
                shaderId;
        }

        public static void SupportsSpirv(this GL gl) {
            Console.WriteLine($"{Spirv} supported: {Contains(gl, Spirv)}");
        }

        public static void BindBufferData<T>(this GL gl,
            BufferTargetARB bufferTarget,
            Span<T> data,
            BufferUsageARB usage,
            ref uint identifier) where T : unmanaged {
            unsafe {
                identifier = gl.GenBuffer();
                gl.BindBuffer(bufferTarget, identifier);
                fixed (void* ptr = data) {
                    gl.BufferData(bufferTarget, (nuint)(data.Length * Marshal.SizeOf<T>()), ptr, usage);
                }
            }
        }

        public static bool Contains(this GL gl, string s) {
            var numExtensions = gl.GetInteger(GLEnum.NumExtensions);

            for (var i = 0; i < numExtensions; i++) {
                var ext = gl.GetStringS(StringName.Extensions, (uint)i);
                if (ext == s) {
                    return true;
                }
            }
            return false;
        }
    }
}

