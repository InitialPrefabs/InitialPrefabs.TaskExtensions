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

    public class Program {
        private static IWindow MainWindow;
        private static GL OpenGl;
        private static uint VAO;
        private static uint VBO;
        private static uint EBO;

        private static uint ShaderProgram;

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
            MainWindow.Closing += OnClose;
            MainWindow.Run();
        }

        private static unsafe void OnClose() {
            OpenGl.DeleteBuffer(VBO);
            OpenGl.DeleteBuffer(EBO);
            OpenGl.DeleteVertexArray(VAO);
            OpenGl.DeleteShader(ShaderProgram);
        }

        private static unsafe void OnLoad() {
            OpenGl = MainWindow.CreateOpenGL();
            OpenGl.ClearColor(Color.CornflowerBlue);
            OpenGl.SupportsSpirv();

            VAO = OpenGl.GenVertexArray();
            OpenGl.BindVertexArray(VAO);

            Span<float> vertices = [
                 0.5f,  0.5f, 0.0f,
                 0.5f, -0.5f, 0.0f,
                -0.5f, -0.5f, 0.0f,
                -0.5f,  0.5f, 0.0f
            ];
            OpenGl.BindBufferData(
                BufferTargetARB.ArrayBuffer,
                vertices,
                BufferUsageARB.StaticDraw,
                ref VBO);

            Span<uint> indices = [0, 1, 3, 1, 2, 3];
            OpenGl.BindBufferData(
                BufferTargetARB.ElementArrayBuffer,
                indices,
                BufferUsageARB.StaticDraw,
                ref EBO);

            var vert = OpenGl.CompileAndLoadShader(ShaderType.VertexShader, VertexShader);
            var frag = OpenGl.CompileAndLoadShader(ShaderType.FragmentShader, FragmentShader);

            ShaderProgram = OpenGl.CreateProgram();
            OpenGl.AttachShader(ShaderProgram, vert);
            OpenGl.AttachShader(ShaderProgram, frag);
            OpenGl.LinkProgram(ShaderProgram);

            OpenGl.GetProgram(ShaderProgram, ProgramPropertyARB.LinkStatus, out var lStatus);
            if (lStatus != (int)GLEnum.True) {
                throw new InvalidOperationException($"Failed to create and link the shader program.\n {OpenGl.GetProgramInfoLog(ShaderProgram)}");
            }

            // Detatch the shader and free the memory
            OpenGl.DetachShader(ShaderProgram, vert);
            OpenGl.DetachShader(ShaderProgram, frag);
            OpenGl.DeleteShader(vert);
            OpenGl.DeleteShader(frag);

            const int positionLoc = 0;
            OpenGl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
            OpenGl.EnableVertexAttribArray(positionLoc);
        }

        private static void OnUpdate(double deltaTime) {
        }

        private static unsafe void OnRender(double deltaTime) {
            OpenGl.Clear(ClearBufferMask.ColorBufferBit);
            OpenGl.BindVertexArray(VAO);
            OpenGl.UseProgram(ShaderProgram);
            OpenGl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
        }
    }
}
