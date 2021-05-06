using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;

namespace Cubach.Client
{
    public static class Program
    {
        private const float GRID_GEN_DISTANCE = 512f - 128f;
        private const float GRID_UNLOAD_DISTANCE = 512f;

        private const float MESH_GEN_DISTANCE = 512f - 128f;
        private const float MESH_UNLOAD_DISTANCE = 512f;

        private const float MAX_RENDER_DISTANCE = 512f;

        private const int MAX_MESH_GEN_PER_FRAME = 8;

        public static bool Exiting = false;
        public readonly static ManualResetEvent WorldGenBarrier = new ManualResetEvent(false);
        public readonly static ConcurrentQueue<Vector3i> GridsToUpdate = new ConcurrentQueue<Vector3i>();
        public static Thread WorldGenThread;

        public static GameWindow Window;

        public static WorldGen WorldGen;
        public static World World;
        public static Camera Camera = new Camera(new Vector3(0, 64, 0), Quaternion.FromAxisAngle(Vector3.UnitY, -3 * MathF.PI / 4));

        public readonly static Dictionary<Vector3i, Mesh<VertexP3N3T2>> WorldOpaqueMeshes = new Dictionary<Vector3i, Mesh<VertexP3N3T2>>();
        public readonly static Dictionary<Vector3i, Mesh<VertexP3N3T2>> WorldTransparentMeshes = new Dictionary<Vector3i, Mesh<VertexP3N3T2>>();

        public static ShaderProgram WorldShader;
        public static ShaderProgram LineShader;
        public static int TextureHandle;
        public static Mesh<VertexP3C4> Lines;

        public static void Main(string[] args)
        {
            using (Window = new GameWindow(new GameWindowSettings(), new NativeWindowSettings() { Title = "Cubach", Size = new Vector2i(1600, 900) }))
            {
                Window.Load += Window_Load;
                Window.Resize += Window_Resize;
                Window.UpdateFrame += Window_UpdateFrame;
                Window.RenderFrame += Window_RenderFrame;
                Window.Closed += Window_Closed;
                Window.Run();
            }

            Exiting = true;

            WorldGenBarrier.Set();
        }

        private static void WorldGenCallback()
        {
            while (!Exiting)
            {
                WorldGenBarrier.WaitOne();

                if (GridsToUpdate.IsEmpty)
                {
                    WorldGenBarrier.Reset();
                    continue;
                }

                while (GridsToUpdate.TryDequeue(out Vector3i gridPosition))
                {
                    GenGrid(gridPosition);
                }

                WorldGenBarrier.Reset();
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

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            WorldGenThread = new Thread(new ThreadStart(WorldGenCallback));
            WorldGenThread.Start();

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
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(image, 0, 0, image.Width, image.Height);

                BitmapData data = copy.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                copy.UnlockBits(data);
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            string vertexShaderSource = @"#version 400
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
}";

            string fragmentShaderSource = @"#version 400
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
}";

            using var vertexShader = new Shader(ShaderType.VertexShader);
            if (!vertexShader.Compile(vertexShaderSource))
            {
                Console.WriteLine(vertexShader.GetError());
            }

            using var fragmentShader = new Shader(ShaderType.FragmentShader);
            if (!fragmentShader.Compile(fragmentShaderSource))
            {
                Console.WriteLine(fragmentShader.GetError());
            }

            WorldShader = new ShaderProgram();
            WorldShader.Attach(vertexShader);
            WorldShader.Attach(fragmentShader);
            if (!WorldShader.Link())
            {
                Console.WriteLine(WorldShader.GetError());
            }

            string lineVertexShaderSource = @"#version 400
#extension GL_ARB_explicit_uniform_location : enable

layout (location = 0) uniform mat4 mvp;

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec4 in_color;

out vec4 frag_color;

void main(void) {
  frag_color = in_color;
  gl_Position = mvp * vec4(in_position, 1);
}";

            string lineFragmentShaderSource = @"#version 400
#extension GL_ARB_explicit_uniform_location : enable

in vec4 frag_color;

layout (location = 0) out vec4 out_color;

void main(void) {
  out_color = frag_color;
}";

            using var lineVertexShader = new Shader(ShaderType.VertexShader);
            if (!lineVertexShader.Compile(lineVertexShaderSource))
            {
                Console.WriteLine(lineVertexShader.GetError());
            }

            using var lineFragmentShader = new Shader(ShaderType.FragmentShader);
            if (!lineFragmentShader.Compile(lineFragmentShaderSource))
            {
                Console.WriteLine(lineFragmentShader.GetError());
            }

            LineShader = new ShaderProgram();
            LineShader.Attach(lineVertexShader);
            LineShader.Attach(lineFragmentShader);
            if (!LineShader.Link())
            {
                Console.WriteLine(LineShader.GetError());
            }

            Lines = new Mesh<VertexP3C4>(new VertexP3C4[] { });
            Lines.SetVertexAttribs(VertexP3C4.VertexAttribs);

            GL.Viewport(0, 0, Window.ClientSize.X, Window.ClientSize.Y);
        }

        private static void Window_Resize(ResizeEventArgs obj)
        {
            GL.Viewport(0, 0, Window.ClientSize.X, Window.ClientSize.Y);
        }

        private static float PreviousFPS = 75;

        private static void Window_UpdateFrame(FrameEventArgs obj)
        {
            float moveSpeed = 10f;

            if (Window.KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                moveSpeed *= 2;
            }

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
            return WorldGen.GRID_SIZE * gridPosition + Vector3.One * WorldGen.GRID_SIZE / 2;
        }

        private static void GenGrid(Vector3i gridPosition)
        {
            if (World.Grids.ContainsKey(gridPosition))
            {
                return;
            }

            World.GenGridAt(gridPosition);

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
            if (!WorldOpaqueMeshes.ContainsKey(grid.Position))
            {
                WorldOpaqueMeshes[grid.Position] = new Mesh<VertexP3N3T2>(grid.GenVertexes());
                WorldOpaqueMeshes[grid.Position].SetVertexAttribs(VertexP3N3T2.VertexAttribs);

                Console.WriteLine($"Generated opaque grid mesh {grid.Position}");
            }

            if (!WorldTransparentMeshes.ContainsKey(grid.Position))
            {
                WorldTransparentMeshes[grid.Position] = new Mesh<VertexP3N3T2>(grid.GenVertexes(opaque: false));
                WorldTransparentMeshes[grid.Position].SetVertexAttribs(VertexP3N3T2.VertexAttribs);

                Console.WriteLine($"Generated transparent grid mesh {grid.Position}");
            }
        }

        private static void UnloadGridMesh(Vector3i gridPosition)
        {
            if (WorldOpaqueMeshes.TryGetValue(gridPosition, out Mesh<VertexP3N3T2> opaqueMesh))
            {
                WorldOpaqueMeshes.Remove(gridPosition);
                opaqueMesh.Dispose();

                Console.WriteLine($"Unloaded opaque grid mesh {gridPosition}");
            }

            if (WorldTransparentMeshes.TryGetValue(gridPosition, out Mesh<VertexP3N3T2> transparentMesh))
            {
                WorldTransparentMeshes.Remove(gridPosition);
                transparentMesh.Dispose();

                Console.WriteLine($"Unloaded transparent grid mesh {gridPosition}");
            }
        }

        private static void GenNearGrids()
        {
            const int minHeight = -1;
            const int maxHeight = 5;

            Vector3i cameraPosition = (Vector3i)(Camera.Position / WorldGen.GRID_SIZE);
            int genDistance = (int)MathF.Floor(GRID_GEN_DISTANCE / WorldGen.GRID_SIZE);

            for (int i = cameraPosition.X - genDistance; i < cameraPosition.X + genDistance; ++i)
            {
                for (int j = Math.Max(cameraPosition.Y - genDistance, minHeight); j < Math.Min(cameraPosition.Y + genDistance, maxHeight); ++j)
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

                        GridsToUpdate.Enqueue(gridPosition);
                    }
                }
            }

            if (!GridsToUpdate.IsEmpty)
            {
                WorldGenBarrier.Set();
            }
        }

        private static void UnloadFarGrids()
        {
            foreach (Vector3i gridPosition in World.Grids.Keys)
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
            List<Vector3i> gridMeshesToUpdate = new List<Vector3i>();

            foreach (Vector3i gridPosition in World.Grids.Keys)
            {
                if (WorldOpaqueMeshes.ContainsKey(gridPosition))
                {
                    continue;
                }

                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance < MESH_GEN_DISTANCE)
                {
                    gridMeshesToUpdate.Add(gridPosition);
                }
            }

            gridMeshesToUpdate.Sort((Vector3i a, Vector3i b) =>
            {
                Vector3 aGridCenter = GetGridCenter(a);
                Vector3 bGridCenter = GetGridCenter(b);

                float aDistance = Coords.TaxicabDistance3(aGridCenter, Camera.Position);
                float bDistance = Coords.TaxicabDistance3(bGridCenter, Camera.Position);

                return MathF.Sign(aDistance - bDistance);
            });

            foreach (Vector3i gridPosition in gridMeshesToUpdate.Take(MAX_MESH_GEN_PER_FRAME).ToArray())
            {
                if (World.Grids.TryGetValue(gridPosition, out Grid grid))
                {
                    GenGridMesh(grid);
                }
            }
        }

        private static void UnloadFarGridMeshes()
        {
            foreach (Vector3i gridPosition in WorldOpaqueMeshes.Keys)
            {
                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance > MESH_UNLOAD_DISTANCE)
                {
                    UnloadGridMesh(gridPosition);
                }
            }
        }

        private static VertexP3C4[] GenLineVertexes(Vector3 a, Vector3 b, Color4 color, float width = 1f)
        {
            Vector3 d = (b - a).Normalized();
            Vector3 p1 = Coords.GetPerpendicular(d);
            Vector3 p2 = Vector3.Cross(d, p1);

            p1 *= (width / 2);
            p2 *= (width / 2);

            return new[] {
                new VertexP3C4(a + p1, color),
                new VertexP3C4(b + p1, color),
                new VertexP3C4(b - p1, color),

                new VertexP3C4(a + p1, color),
                new VertexP3C4(b - p1, color),
                new VertexP3C4(a - p1, color),

                new VertexP3C4(a + p2, color),
                new VertexP3C4(b + p2, color),
                new VertexP3C4(b - p2, color),

                new VertexP3C4(a + p2, color),
                new VertexP3C4(b - p2, color),
                new VertexP3C4(a - p2, color),
            };
        }

        private static void Window_RenderFrame(FrameEventArgs obj)
        {
            GenNearGrids();
            UnloadFarGrids();

            GenNearGridMeshes();
            UnloadFarGridMeshes();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            WorldShader.Use();

            Matrix4 View = Camera.ViewMatrix;
            Matrix4 Projection = Matrix4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)Window.ClientSize.X / Window.ClientSize.Y, 0.1f, 512f);
            Matrix4 ViewProjection = View * Projection;

            GL.Uniform1(1, 0);
            GL.Uniform3(2, new Vector3(0.2f, 1, 0.1f).Normalized());

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);

            foreach ((Vector3i gridPosition, var mesh) in WorldOpaqueMeshes)
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

            var transparentMeshes = new List<(Vector3i, Mesh<VertexP3N3T2>)>();

            foreach ((Vector3i gridPosition, var mesh) in WorldTransparentMeshes)
            {
                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance > MAX_RENDER_DISTANCE)
                {
                    continue;
                }

                if (mesh.VertexCount != 0)
                {
                    transparentMeshes.Add((gridPosition, mesh));
                }
            }

            transparentMeshes.Sort((a, b) =>
            {
                Vector3 aGridCenter = GetGridCenter(a.Item1);
                Vector3 bGridCenter = GetGridCenter(b.Item1);

                float aDistance = Coords.TaxicabDistance3(aGridCenter, Camera.Position);
                float bDistance = Coords.TaxicabDistance3(bGridCenter, Camera.Position);

                return MathF.Sign(bDistance - aDistance);
            });

            foreach ((Vector3i gridPosition, var mesh) in transparentMeshes)
            {
                Matrix4 Model = Matrix4.CreateTranslation(16 * gridPosition);
                Matrix4 ModelViewProjection = Model * ViewProjection;
                GL.UniformMatrix4(0, false, ref ModelViewProjection);
                mesh.Draw();
            }

            GL.Disable(EnableCap.CullFace);

            LineShader.Use();
            GL.UniformMatrix4(0, false, ref ViewProjection);

            var lineVertexes = new List<VertexP3C4>();
            var ray = new Ray(Camera.Position, Camera.Front);
            float minDistance = 0f;
            float maxDistance = 128f;
            while (minDistance < maxDistance)
            {
                Grid grid = World.RaycastGrid(ray, minDistance, maxDistance, out Vector3 gridIntersection);
                if (grid == null)
                {
                    break;
                }

                Block? block = grid.RaycastBlock(ray, out Vector3 blockIntersection, out Vector3i blockPosition);
                if (block.HasValue)
                {
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(0, 0, 0), blockPosition + new Vector3(1, 0, 0), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(1, 0, 0), blockPosition + new Vector3(1, 1, 0), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(1, 1, 0), blockPosition + new Vector3(0, 1, 0), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(0, 1, 0), blockPosition + new Vector3(0, 0, 0), Color4.LightGray, 0.025f));

                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(0, 0, 1), blockPosition + new Vector3(1, 0, 1), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(1, 0, 1), blockPosition + new Vector3(1, 1, 1), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(1, 1, 1), blockPosition + new Vector3(0, 1, 1), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(0, 1, 1), blockPosition + new Vector3(0, 0, 1), Color4.LightGray, 0.025f));

                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(0, 0, 0), blockPosition + new Vector3(0, 0, 1), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(1, 0, 0), blockPosition + new Vector3(1, 0, 1), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(1, 1, 0), blockPosition + new Vector3(1, 1, 1), Color4.LightGray, 0.025f));
                    lineVertexes.AddRange(GenLineVertexes(blockPosition + new Vector3(0, 1, 0), blockPosition + new Vector3(0, 1, 1), Color4.LightGray, 0.025f));
                    break;
                }

                minDistance = (gridIntersection - ray.Origin).Length + 0.1f;
            }

            if (lineVertexes.Count > 0)
            {
                Lines.SetData(lineVertexes.ToArray(), BufferUsageHint.DynamicDraw);
                Lines.Draw();
            }

            GL.Enable(EnableCap.CullFace);

            Window.SwapBuffers();
        }

        private static void Window_Closed()
        {
            foreach (var mesh in WorldOpaqueMeshes.Values)
            {
                mesh.Dispose();
            }

            foreach (var mesh in WorldTransparentMeshes.Values)
            {
                mesh.Dispose();
            }

            GL.DeleteTexture(TextureHandle);

            WorldShader.Dispose();
            LineShader.Dispose();

            Lines.Dispose();
        }
    }
}
