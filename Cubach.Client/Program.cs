using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace Cubach.Client
{
    public static class Program
    {
        private const float GRID_GEN_DISTANCE = 384f - 64f;
        private const float GRID_UNLOAD_DISTANCE = 384f;

        private const float MESH_GEN_DISTANCE = 384f - 64f;
        private const float MESH_UNLOAD_DISTANCE = 384f;

        private const float MAX_RENDER_DISTANCE = 384f;

        private const int MAX_GRID_GEN_PER_FRAME = 16;
        private const int MAX_MESH_GEN_PER_FRAME = 4;

        public static GameWindow Window;

        public static WorldGen WorldGen;
        public static World World;
        public static Camera Camera = new Camera(new Vector3(0, 32, 0), Quaternion.FromAxisAngle(Vector3.UnitY, -3 * MathF.PI / 4));

        public readonly static Dictionary<Vector3i, Mesh<VertexP3N3T2>> WorldMeshes = new Dictionary<Vector3i, Mesh<VertexP3N3T2>>();

        public static int TextureHandle;
        public static int ShaderProgramHandle;

        public static void Main(string[] args)
        {
            using (Window = new GameWindow(new GameWindowSettings(), new NativeWindowSettings() { Title = "Cubach" }))
            {
                Window.Load += Window_Load;
                Window.Resize += Window_Resize;
                Window.UpdateFrame += Window_UpdateFrame;
                Window.RenderFrame += Window_RenderFrame;
                Window.Closed += Window_Closed;
                Window.Run();
            }
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

            WorldGen = new WorldGen();
            World = new World(WorldGen);

            GenNearGrids();
            GenNearGridMeshes();

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

        private static float PreviousFPS = 75;

        private static void Window_UpdateFrame(FrameEventArgs obj)
        {
            const float moveSpeed = 10f;

            if (Window.KeyboardState.IsKeyDown(Keys.A))
            {
                Camera.Position -= Camera.Right * (float)obj.Time * moveSpeed;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.D))
            {
                Camera.Position += Camera.Right * (float)obj.Time * moveSpeed;
            }

            if (Window.KeyboardState.IsKeyDown(Keys.W))
            {
                Camera.Position += Camera.Front * (float)obj.Time * moveSpeed;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.S))
            {
                Camera.Position -= Camera.Front * (float)obj.Time * moveSpeed;
            }

            if (Window.KeyboardState.IsKeyDown(Keys.R))
            {
                Camera.Position += Camera.Up * (float)obj.Time * moveSpeed;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.F))
            {
                Camera.Position -= Camera.Up * (float)obj.Time * moveSpeed;
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
                Camera.Rotation = Quaternion.FromAxisAngle(Camera.Right, (float)obj.Time) * Camera.Rotation;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.Down))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Camera.Right, -(float)obj.Time) * Camera.Rotation;
            }

            Camera.Rotation.Normalize();

            if (Window.RenderTime > 0)
            {
                float fps = (float)(PreviousFPS * 0.9f + 1f / Window.RenderTime * 0.1f);
                PreviousFPS = fps;
            }

            Window.Title = $"Cubach - {PreviousFPS.ToString("N2")} FPS";
        }

        private static Vector3 GetGridCenter(Vector3i gridPosition)
        {
            return WorldGen.GridSize * gridPosition + Vector3.One * WorldGen.GridSize / 2;
        }

        private static void GenGrid(Vector3i gridPosition)
        {
            if (World.Grids.ContainsKey(gridPosition))
            {
                return;
            }

            World.GenGrid(gridPosition);

            Console.WriteLine($"Generated grid {gridPosition}");
        }

        private static void UnloadGrid(Vector3i gridPosition)
        {
            if (!World.Grids.ContainsKey(gridPosition))
            {
                return;
            }

            if (World.Grids.TryRemove(gridPosition, out Grid grid))
            {
                Console.WriteLine($"Unloaded grid {gridPosition}");
            }
        }

        private static void GenGridMesh(Grid grid)
        {
            if (WorldMeshes.ContainsKey(grid.Position))
            {
                return;
            }

            WorldMeshes[grid.Position] = new Mesh<VertexP3N3T2>(grid.GenVertexes());
            WorldMeshes[grid.Position].SetVertexAttribs(VertexP3N3T2.VertexAttribs);

            Console.WriteLine($"Generated grid mesh {grid.Position}");
        }

        private static void UnloadGridMesh(Vector3i gridPosition)
        {
            if (WorldMeshes.TryGetValue(gridPosition, out Mesh<VertexP3N3T2> mesh))
            {
                WorldMeshes.Remove(gridPosition);
                mesh.Dispose();

                Console.WriteLine($"Unloaded grid mesh {gridPosition}");
            }
        }

        private static void GenNearGrids()
        {
            var gridPositions = new List<Vector3i>(MAX_GRID_GEN_PER_FRAME);
            Vector3i cameraPosition = (Vector3i)(Camera.Position / 16);
            int genDistance = (int)MathF.Floor(GRID_GEN_DISTANCE / 16) / 3;
            int n = 0;

            for (int i = cameraPosition.X - genDistance; i < cameraPosition.X + genDistance; ++i)
            {
                for (int j = cameraPosition.Y - genDistance; j < cameraPosition.Y + genDistance; ++j)
                {
                    for (int k = cameraPosition.Z - genDistance; k < cameraPosition.Z + genDistance; ++k)
                    {
                        var gridPosition = new Vector3i(i, j, k);
                        if (World.Grids.ContainsKey(gridPosition))
                        {
                            continue;
                        }

                        Vector3 gridCenter = GetGridCenter(gridPosition);
                        float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                        if (distance > GRID_GEN_DISTANCE)
                        {
                            continue;
                        }

                        if (n++ > MAX_GRID_GEN_PER_FRAME)
                        {
                            continue;
                        }

                        gridPositions.Add(gridPosition);
                    }
                }
            }

            if (gridPositions.Count > 0)
            {
                ThreadPool.QueueUserWorkItem((obj) =>
                {
                    foreach (Vector3i gridPosition in gridPositions)
                    {
                        GenGrid(gridPosition);
                    }
                });
            }
        }

        private static void UnloadFarGrids()
        {
            foreach ((Vector3i gridPosition, Grid grid) in World.Grids)
            {
                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance > GRID_UNLOAD_DISTANCE)
                {
                    UnloadGrid(gridPosition);
                }
            }
        }

        private static void GenNearGridMeshes()
        {
            int n = 0;

            foreach ((Vector3i gridPosition, Grid grid) in World.Grids)
            {
                if (WorldMeshes.ContainsKey(gridPosition))
                {
                    continue;
                }

                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance < MESH_GEN_DISTANCE)
                {
                    GenGridMesh(grid);

                    if (n++ > MAX_MESH_GEN_PER_FRAME)
                    {
                        return;
                    }
                }
            }
        }

        private static void UnloadFarGridMeshes()
        {
            foreach ((Vector3i gridPosition, var mesh) in WorldMeshes)
            {
                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance > MESH_UNLOAD_DISTANCE)
                {
                    UnloadGridMesh(gridPosition);
                }
            }
        }

        private static void Window_RenderFrame(FrameEventArgs obj)
        {
            GenNearGrids();
            UnloadFarGrids();

            GenNearGridMeshes();
            UnloadFarGridMeshes();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(ShaderProgramHandle);
            Matrix4 View = Camera.ViewMatrix;
            Matrix4 Projection = Matrix4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)Window.ClientSize.X / Window.ClientSize.Y, 0.1f, 1000);
            Matrix4 ViewProjection = View * Projection;

            GL.Uniform1(1, 0);
            GL.Uniform3(2, new Vector3(0.2f, 1, 0.1f).Normalized());

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);

            foreach ((Vector3i gridPosition, var mesh) in WorldMeshes)
            {
                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance > MAX_RENDER_DISTANCE)
                {
                    continue;
                }

                Matrix4 Model = Matrix4.CreateTranslation(16 * gridPosition);
                Matrix4 ModelViewProjection = Model * ViewProjection;
                GL.UniformMatrix4(0, false, ref ModelViewProjection);
                mesh.Draw();
            }

            Window.SwapBuffers();
        }

        private static void Window_Closed()
        {
            foreach (var mesh in WorldMeshes.Values)
            {
                mesh.Dispose();
            }

            GL.DeleteTexture(TextureHandle);
            GL.DeleteProgram(ShaderProgramHandle);
        }
    }
}
