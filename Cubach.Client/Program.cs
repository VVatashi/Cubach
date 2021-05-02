using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cubach.Client
{
    public static class Program
    {
        public static GameWindow Window;

        public static Camera Camera = new Camera { Position = new Vector3(18, 18, 18), Rotation = Quaternion.Identity };

        public static int VertexArrayHandle;
        public static int VertexBufferHandle;
        public static int VertexCount;
        public static int TextureHandle;
        public static int ShaderProgramHandle;

        public static Dictionary<byte, BlockType> BlockTypes = new Dictionary<byte, BlockType>
        {
            [0] = new BlockType { GenGeometry = false, TextureId = 0, TopTextureId = 0, BottomTextureId = 0 },
            [1] = new BlockType { GenGeometry = true, TextureId = 1, TopTextureId = 1, BottomTextureId = 1 },
            [2] = new BlockType { GenGeometry = true, TextureId = 2, TopTextureId = 0, BottomTextureId = 1 },
        };

        public static void Main(string[] args)
        {
            using (Window = new GameWindow(new GameWindowSettings(), new NativeWindowSettings()))
            {
                Window.Load += Window_Load;
                Window.Resize += Window_Resize;
                Window.UpdateFrame += Window_UpdateFrame;
                Window.RenderFrame += Window_RenderFrame;
                Window.Closed += Window_Closed;
                Window.Run();
            }
        }

        public static float[] GenGridVertexes(byte[,,] blocks)
        {
            const int blockVertexCount = 36;

            var result = new List<float>(blocks.Length * blockVertexCount);

            for (int i = 0; i < blocks.GetLength(0); ++i)
            {
                for (int j = 0; j < blocks.GetLength(1); ++j)
                {
                    for (int k = 0; k < blocks.GetLength(2); ++k)
                    {
                        float[] blockVertexes = GenBlockVertexes(blocks[i, j, k], i, j, k);
                        result.AddRange(blockVertexes);
                    }
                }
            }

            return result.ToArray();
        }

        public static float[] GenBlockVertexes(byte blockTypeId, int x, int y, int z)
        {
            return GenBlockVertexes(BlockTypes[blockTypeId], x, y, z);
        }

        public static float[] GenBlockVertexes(BlockType blockType, int x, int y, int z)
        {
            if (!blockType.GenGeometry)
            {
                return new float[0];
            }

            float u1 = (blockType.TextureId % 16) / 16f;
            float u2 = (blockType.TextureId % 16 + 1) / 16f;
            float v1 = (blockType.TextureId / 16) / 16f;
            float v2 = (blockType.TextureId / 16 + 1) / 16f;

            float topU1 = (blockType.TopTextureId % 16) / 16f;
            float topU2 = (blockType.TopTextureId % 16 + 1) / 16f;
            float topV1 = (blockType.TopTextureId / 16) / 16f;
            float topV2 = (blockType.TopTextureId / 16 + 1) / 16f;

            float bottomU1 = (blockType.BottomTextureId % 16) / 16f;
            float bottomU2 = (blockType.BottomTextureId % 16 + 1) / 16f;
            float bottomV1 = (blockType.BottomTextureId / 16) / 16f;
            float bottomV2 = (blockType.BottomTextureId / 16 + 1) / 16f;

            return new float[] {
                // -Z
                    x,     y, z,   0, 0, -1,   u2, v2,
                x + 1,     y, z,   0, 0, -1,   u1, v2,
                x + 1, y + 1, z,   0, 0, -1,   u1, v1,

                    x, y,     z,   0, 0, -1,   u2, v2,
                x + 1, y + 1, z,   0, 0, -1,   u1, v1,
                    x, y + 1, z,   0, 0, -1,   u2, v1,

                // -Y
                    x, y,     z,   0, -1, 0,   bottomU1, bottomV1,
                x + 1, y, z + 1,   0, -1, 0,   bottomU2, bottomV2,
                x + 1, y,     z,   0, -1, 0,   bottomU2, bottomV1,

                    x, y,     z,   0, -1, 0,   bottomU1, bottomV1,
                    x, y, z + 1,   0, -1, 0,   bottomU1, bottomV2,
                x + 1, y, z + 1,   0, -1, 0,   bottomU2, bottomV2,

                // -X
                x,     y,     z,   -1, 0, 0,   u1, v2,
                x, y + 1,     z,   -1, 0, 0,   u1, v1,
                x, y + 1, z + 1,   -1, 0, 0,   u2, v1,

                x,     y,     z,   -1, 0, 0,   u1, v2,
                x, y + 1, z + 1,   -1, 0, 0,   u2, v1,
                x,     y, z + 1,   -1, 0, 0,   u2, v2,

                // Z
                    x,     y, z + 1,   0, 0, 1,   u1, v2,
                x + 1, y + 1, z + 1,   0, 0, 1,   u2, v1,
                x + 1,     y, z + 1,   0, 0, 1,   u2, v2,

                    x,     y, z + 1,   0, 0, 1,   u1, v2,
                    x, y + 1, z + 1,   0, 0, 1,   u1, v1,
                x + 1, y + 1, z + 1,   0, 0, 1,   u2, v1,

                // Y
                    x, y + 1,     z,   0, 1, 0,   topU1, topV1,
                x + 1, y + 1,     z,   0, 1, 0,   topU2, topV1,
                x + 1, y + 1, z + 1,   0, 1, 0,   topU2, topV2,

                    x, y + 1,     z,   0, 1, 0,   topU1, topV1,
                x + 1, y + 1, z + 1,   0, 1, 0,   topU2, topV2,
                    x, y + 1, z + 1,   0, 1, 0,   topU1, topV2,

                // X
                x + 1,     y,     z,   1, 0, 0,   u2, v2,
                x + 1, y + 1, z + 1,   1, 0, 0,   u1, v1,
                x + 1, y + 1,     z,   1, 0, 0,   u2, v1,

                x + 1,     y,     z,   1, 0, 0,   u2, v2,
                x + 1,     y, z + 1,   1, 0, 0,   u1, v2,
                x + 1, y + 1, z + 1,   1, 0, 0,   u1, v1,
            };
        }

        private static void Window_Load()
        {
            GL.ClearColor(Color4.Black);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            GL.FrontFace(FrontFaceDirection.Cw);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            VertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);

            byte[,,] grid = new byte[16, 16, 16];
            for (int i = 0; i < grid.GetLength(0); ++i)
            {
                for (int k = 0; k < grid.GetLength(2); ++k)
                {
                    float noise = (PerlinNoise.Noise(0.1f * new Vector2(i, k)) + 1) / 2;
                    int maxHeight = 8 + (int)Math.Floor(8 * noise);

                    for (int j = 0; j < grid.GetLength(1); ++j)
                    {
                        if (j < maxHeight)
                        {
                            grid[i, j, k] = 1;
                        }
                        else if (j == maxHeight)
                        {
                            grid[i, j, k] = 2;
                        }
                        else
                        {
                            grid[i, j, k] = 0;
                        }
                    }
                }
            }

            float[] vertexes = GenGridVertexes(grid);
            VertexCount = vertexes.Length / 8;

            GL.BufferData(BufferTarget.ArrayBuffer, Marshal.SizeOf<float>() * vertexes.Length, vertexes, BufferUsageHint.StaticDraw);

            VertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(VertexBufferHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferHandle);

            // Position.
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<float>() * 8, 0);

            // Normal.
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, Marshal.SizeOf<float>() * 8, Marshal.SizeOf<float>() * 3);

            // Tex coords.
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, true, Marshal.SizeOf<float>() * 8, Marshal.SizeOf<float>() * 6);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.BindVertexArray(0);

            TextureHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 4);

            using (var image = new Bitmap("./blocks.png"))
            using (var copy = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(copy))
            {
                graphics.DrawImage(image, 0, 0, image.Width, image.Height);

                BitmapData data = copy.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                copy.UnlockBits(data);
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            ShaderProgramHandle = GL.CreateProgram();

            int vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShaderHandle, @"#version 400
#extension GL_ARB_explicit_uniform_location : enable

layout (location = 0) uniform mat4 mvp;

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec3 in_normal;
layout (location = 2) in vec2 in_texCoord;

out vec3 frag_position;
out vec3 frag_normal;
out vec2 frag_texCoord;

void main(void) {
  frag_position = in_position;
  frag_normal = in_normal;
  frag_texCoord = in_texCoord;
  gl_Position = mvp * vec4(in_position, 1);
}");
            GL.CompileShader(vertexShaderHandle);

            GL.GetShader(vertexShaderHandle, ShaderParameter.CompileStatus, out int vertexShaderStatus);
            if (vertexShaderStatus == 0)
            {
                Console.WriteLine(GL.GetShaderInfoLog(vertexShaderHandle));
            }

            int fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShaderHandle, @"#version 400
#extension GL_ARB_explicit_uniform_location : enable

layout (location = 1) uniform sampler2D colorTexture;
layout (location = 2) uniform vec3 light;

in vec3 frag_position;
in vec3 frag_normal;
in vec2 frag_texCoord;

layout (location = 0) out vec4 out_color;

void main(void) {
  vec3 ambient = vec3(0.5);
  vec3 diffuse = max(0, dot(light, frag_normal)) * vec3(0.5);
  out_color = vec4(ambient + diffuse, 1) * texture(colorTexture, frag_texCoord);
}");
            GL.CompileShader(fragmentShaderHandle);

            GL.GetShader(fragmentShaderHandle, ShaderParameter.CompileStatus, out int fragmentShaderStatus);
            if (fragmentShaderStatus == 0)
            {
                Console.WriteLine(GL.GetShaderInfoLog(fragmentShaderHandle));
            }

            GL.AttachShader(ShaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(ShaderProgramHandle, fragmentShaderHandle);
            GL.LinkProgram(ShaderProgramHandle);

            GL.GetProgram(ShaderProgramHandle, GetProgramParameterName.LinkStatus, out int shaderProgramStatus);
            if (shaderProgramStatus == 0)
            {
                Console.WriteLine(GL.GetProgramInfoLog(ShaderProgramHandle));
            }

            GL.DeleteShader(vertexShaderHandle);

            GL.Viewport(0, 0, Window.ClientSize.X, Window.ClientSize.Y);
        }

        private static void Window_Resize(ResizeEventArgs obj)
        {
            GL.Viewport(0, 0, Window.ClientSize.X, Window.ClientSize.Y);
        }

        private static void Window_UpdateFrame(FrameEventArgs obj)
        {
            const float moveSpeed = 10f;

            if (Window.KeyboardState.IsKeyDown(Keys.A))
            {
                Camera.Position -= Camera.GetRight() * (float)obj.Time * moveSpeed;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.D))
            {
                Camera.Position += Camera.GetRight() * (float)obj.Time * moveSpeed;
            }

            if (Window.KeyboardState.IsKeyDown(Keys.W))
            {
                Camera.Position += Camera.GetFront() * (float)obj.Time * moveSpeed;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.S))
            {
                Camera.Position -= Camera.GetFront() * (float)obj.Time * moveSpeed;
            }

            if (Window.KeyboardState.IsKeyDown(Keys.R))
            {
                Camera.Position += Camera.GetUp() * (float)obj.Time * moveSpeed;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.F))
            {
                Camera.Position -= Camera.GetUp() * (float)obj.Time * moveSpeed;
            }

            if (Window.KeyboardState.IsKeyDown(Keys.Left))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, (float)obj.Time) * Camera.Rotation;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.Right))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, -(float)obj.Time) * Camera.Rotation;
            }

            if (Window.KeyboardState.IsKeyDown(Keys.Up))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Camera.GetRight(), (float)obj.Time) * Camera.Rotation;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.Down))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Camera.GetRight(), -(float)obj.Time) * Camera.Rotation;
            }

            Camera.Rotation.Normalize();
        }

        private static void Window_RenderFrame(FrameEventArgs obj)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(ShaderProgramHandle);
            Matrix4 View = Camera.GetViewMatrix();
            Matrix4 Projection = Matrix4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)Window.ClientSize.X / Window.ClientSize.Y, 0.1f, 100);
            Matrix4 ModelViewProjection = View * Projection;
            GL.UniformMatrix4(0, false, ref ModelViewProjection);

            GL.Uniform1(1, 0);
            GL.Uniform3(2, new Vector3(0.2f, 1, 0.1f).Normalized());

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);

            GL.BindVertexArray(VertexArrayHandle);
            GL.DrawArrays(PrimitiveType.Triangles, 0, VertexCount);

            Window.SwapBuffers();
        }

        private static void Window_Closed()
        {
            GL.DeleteVertexArray(VertexArrayHandle);
            GL.DeleteBuffer(VertexBufferHandle);
            GL.DeleteProgram(ShaderProgramHandle);
        }
    }
}
