using InitialPrefabs.TaskFlow.Threading;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Drawing;

namespace InitialPrefabs.TaskFlow.Examples {

    internal struct ResetTask : ITaskFor {
        public int[] A;

        public readonly void Execute(int index) {
            A[index] = 0;
        }
    }

    internal struct AddTask : ITaskFor {
        public int[] A;

        public readonly void Execute(int index) {
            A[index] += 1;
        }
    }

    public static class OpenGLExtensions {
        private const string Spirv = "GL_ARB_gl_spirv";

        public static void SupportsSpirv(this GL gl) {
            Console.WriteLine($"{Spirv} supported: {Contains(gl, Spirv)}");
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

    public class Program {
        private static IWindow MainWindow;
        private static GL OpenGl;
        private static uint VAO;
        private static uint VBO;
        private static uint EBO;

        private const string VertexShader = @"
            #version 330 core

            layout (location = 0) in vec3 aPosition;

            void main()
            {
                gl_Position = vec4(aPosition, 1.0);
            }";

        private const string FragmentShader = @"
            #version 330 core

            out vec4 out_color;

            void main()
            {
                out_color = vec4(1.0, 0.5, 0.2, 1.0);
            }";


        public static void Main(string[] argv) {
            new Builder()
                .WithTaskCapacity(64)
                .WithWorkerCapacity(64)
                .WithLogHandler(Console.WriteLine)
                .Build();

            var options = WindowOptions.Default with {
                Size = new Vector2D<int>(800, 600),
                Title = "Demo"
            };

            MainWindow = Window.Create(options);

            MainWindow.Load += OnLoad;
            MainWindow.Update += OnUpdate;
            MainWindow.Render += OnRender;
            MainWindow.Run();
        }

        private static unsafe void OnLoad() {
            OpenGl = MainWindow.CreateOpenGL();
            OpenGl.ClearColor(Color.CornflowerBlue);
            OpenGl.SupportsSpirv();

            VAO = OpenGl.GenVertexArray();
            OpenGl.BindVertexArray(VAO);

            var vertices = stackalloc float[12] {
                 0.5f,  0.5f, 0.0f,
                 0.5f, -0.5f, 0.0f,
                -0.5f, -0.5f, 0.0f,
                -0.5f,  0.5f, 0.0f
            };

            VBO = OpenGl.GenBuffer();
            OpenGl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            OpenGl.BufferData(BufferTargetARB.ArrayBuffer, 12 * sizeof(float), vertices, BufferUsageARB.StaticDraw);

            var indices = stackalloc uint[6] { 0, 1, 3, 1, 2, 3 };
            EBO = OpenGl.GenBuffer();
            OpenGl.BindBuffer(BufferTargetARB.ElementArrayBuffer, EBO);
            OpenGl.BufferData(BufferTargetARB.ElementArrayBuffer, 6 * sizeof(uint), indices, BufferUsageARB.StaticDraw);

            var vert = OpenGl.CreateShader(ShaderType.VertexShader);
            OpenGl.ShaderSource(vert, VertexShader);
        }

        private static void OnUpdate(double deltaTime) {
        }

        private static void OnRender(double deltaTime) {
            OpenGl.Clear(ClearBufferMask.ColorBufferBit);
        }
    }
}
