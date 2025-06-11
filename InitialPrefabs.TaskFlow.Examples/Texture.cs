
using System;
using System.IO;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace InitialPrefabs.TaskFlow.Examples {
    public readonly struct Texture : IDisposable {
        private readonly uint handle;
        private readonly GL gl;

        public unsafe Texture(GL gl, string path) {
            //Saving the gl instance.
            this.gl = gl;

            //Generating the opengl handle;
            handle = this.gl.GenTexture();
            Bind();

            // Load the image from memory.
            ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);

            fixed (byte* ptr = result.Data) {
                // Create our texture and upload the image data.
                this.gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)result.Width,
                        (uint)result.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            SetParameters();
        }

        public unsafe Texture(GL gl, Span<byte> data, uint width, uint height) {
            //Saving the gl instance.
            this.gl = gl;

            //Generating the opengl handle;
            handle = this.gl.GenTexture();
            Bind();

            //We want the ability to create a texture using data generated from code aswell.
            fixed (void* d = &data[0]) {
                //Setting the data of a texture.
                this.gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
                SetParameters();
            }
        }

        private void SetParameters() {
            //Setting some texture perameters so the texture behaves as expected.
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);

            //Generating mipmaps.
            gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0) {
            //When we bind a texture we can choose which textureslot we can bind it to.
            gl.ActiveTexture(textureSlot);
            gl.BindTexture(TextureTarget.Texture2D, handle);
        }

        public void Dispose() {
            //In order to dispose we need to delete the opengl handle for the texure.
            gl.DeleteTexture(handle);
        }
    }
}
