using Silk.NET.OpenGL;
using System;
using System.IO;

namespace InitialPrefabs.TaskFlow.Examples {

    public readonly struct Shader : IDisposable {

        private readonly uint handle;
        private readonly GL gl;

        public Shader(GL gl, string vertexPath, string fragmentPath) {
            this.gl = gl;

            var vertex = LoadShader(ShaderType.VertexShader, vertexPath);
            var fragment = LoadShader(ShaderType.FragmentShader, fragmentPath);

            handle = gl.CreateProgram();
            gl.AttachShader(handle, vertex);
            gl.AttachShader(handle, fragment);
            gl.LinkProgram(handle);

            gl.GetProgram(handle, GLEnum.LinkStatus, out var status);
            if (status == 0) {
                throw new InvalidOperationException($"Program failed to link with error: {gl.GetProgramInfoLog(handle)}");
            }

            gl.DetachShader(handle, vertex);
            gl.DetachShader(handle, fragment);
            gl.DeleteShader(vertex);
            gl.DeleteShader(fragment);
        }

        public readonly Shader Use() {
            gl.UseProgram(handle);
            return this;
        }


        public readonly void SetUniform(string name, int value) {
            int location = gl.GetUniformLocation(handle, name);
            if (location == -1) {
                throw new Exception($"{name} uniform not found on shader.");
            }
            gl.Uniform1(location, value);
        }

        public readonly void SetUniform(string name, float value) {
            int location = gl.GetUniformLocation(handle, name);
            if (location == -1) {
                throw new Exception($"{name} uniform not found on shader.");
            }
            gl.Uniform1(location, value);
        }


        private uint LoadShader(ShaderType shaderType, string path) {
            var src = File.ReadAllText(path);
            var handle = gl.CreateShader(shaderType);
            gl.ShaderSource(handle, src);
            gl.CompileShader(handle);
            var log = gl.GetShaderInfoLog(handle);

            if (!string.IsNullOrWhiteSpace(log)) {
                throw new InvalidOperationException($"Failed to compiling shader of type: {shaderType} due to: {log}");
            }

            return handle;
        }

        public void Dispose() {
            gl.DeleteProgram(handle);
        }
    }
}
