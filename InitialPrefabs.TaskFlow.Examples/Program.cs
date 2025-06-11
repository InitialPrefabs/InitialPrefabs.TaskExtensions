using InitialPrefabs.TaskFlow.Threading;
using InitialPrefabs.TaskFlow.Utils;
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

        private static BufferObject<float> Vertices;
        private static BufferObject<uint> Indices;
        private static VertexArrayObject<float, uint> VAO;
        private static Shader Shader;
        private static Texture Texture;

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
            Vertices.Dispose();
            Indices.Dispose();
            VAO.Dispose();
            Shader.Dispose();
            Texture.Dispose();
        }

        private static unsafe void OnLoad() {
            OpenGl = GL.GetApi(MainWindow);
            OpenGl.ClearColor(Color.CornflowerBlue);

            Span<float> vertices = [
                //X    Y      Z     S    T
                 0.5f,  0.5f, 0.0f, 1.0f, 0.0f,
                 0.5f, -0.5f, 0.0f, 1.0f, 1.0f,
                -0.5f, -0.5f, 0.0f, 0.0f, 1.0f,
                -0.5f,  0.5f, 0.5f, 0.0f, 0.0f
            ];
            Span<uint> indices = [0, 1, 3, 1, 2, 3];

            Vertices = new BufferObject<float>(OpenGl, vertices, BufferTargetARB.ArrayBuffer);
            Indices = new BufferObject<uint>(OpenGl, indices, BufferTargetARB.ElementArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(OpenGl, Vertices, Indices);
            VAO
                .VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0)
                .VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
            Shader = new Shader(OpenGl,
                "InitialPrefabs.TaskFlow.Examples/shaders/vert.glsl",
                "InitialPrefabs.TaskFlow.Examples/shaders/frag.glsl");

            Texture = new Texture(OpenGl, "InitialPrefabs.TaskFlow.Examples/shaders/silk.png");
        }

        private static void OnUpdate(double deltaTime) { }

        private static unsafe void OnRender(double deltaTime) {
            OpenGl.Clear(ClearBufferMask.ColorBufferBit);
            VAO.Bind();
            Shader.Use();

            Texture.Bind(TextureUnit.Texture0);
            //Setting a uniform.
            Shader.SetUniform("uTexture", 0);
            LogUtils.Emit($"Indices Length: {Indices.Length}");
            OpenGl.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, null);
        }
    }
}
