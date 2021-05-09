using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using GDIPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Cubach.Client
{
    public static class Program
    {
        private const float GRID_GEN_DISTANCE = 512f - 128f;
        private const float GRID_UNLOAD_DISTANCE = 512f;

        private const float MESH_GEN_DISTANCE = 512f - 128f;
        private const float MESH_UNLOAD_DISTANCE = 512f;

        private const float MAX_RENDER_DISTANCE = 512f;

        private static float PreviousFPS = 75;
        private static float CurrentTime = 0;
        private static float LastJumpTime = 0;

        public static bool Exiting = false;

        [ThreadStatic]
        public static readonly bool IsMainThread = true;

        public readonly static ManualResetEvent WorldGenBarrier = new ManualResetEvent(false);
        public readonly static ManualResetEvent WorldMeshGenBarrier = new ManualResetEvent(false);

        public readonly static ConcurrentQueue<Vector3i> GridsToUpdate = new ConcurrentQueue<Vector3i>();
        public readonly static ConcurrentQueue<Vector3i> GridMeshesToUpdate = new ConcurrentQueue<Vector3i>();

        public static Thread WorldGenThread;
        public static Thread WorldMeshGenThread;

        public static GameWindow Window;

        public static WorldGen WorldGen = new WorldGen();
        public static World World = new World(WorldGen);
        public static Player Player = new Player(new Vector3(0.1f, 64f, 0.1f));
        public static Camera Camera = new Camera(Player.Position + 0.5f * Vector3.UnitY, Quaternion.Identity);

        public readonly static ConcurrentDictionary<Vector3i, Mesh<VertexP3N3T2>> WorldOpaqueMeshes = new ConcurrentDictionary<Vector3i, Mesh<VertexP3N3T2>>();
        public readonly static ConcurrentDictionary<Vector3i, Mesh<VertexP3N3T2>> WorldTransparentMeshes = new ConcurrentDictionary<Vector3i, Mesh<VertexP3N3T2>>();

        public static ShaderProgram SkyboxShader;
        public static ShaderProgram WorldShader;
        public static ShaderProgram LineShader;

        public static TextureCubemap SkyboxTexture;
        public static Texture2D BlocksTexture;

        public static Mesh<VertexP3> SkyboxMesh;
        public static Mesh<VertexP3C4> LinesMesh;

        public static void Main(string[] args)
        {
            using (Window = new GameWindow(new GameWindowSettings(), new NativeWindowSettings()
            {
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible,
                Title = "Cubach",
                Size = new Vector2i(1600, 900)
            }))
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
            WorldMeshGenBarrier.Set();
        }

        private static void WorldGenCallback()
        {
            while (!Exiting)
            {
                WorldGenBarrier.WaitOne();
                WorldGenBarrier.Reset();

                if (GridsToUpdate.IsEmpty)
                {
                    continue;
                }

                while (!Exiting && GridsToUpdate.TryDequeue(out Vector3i gridPosition))
                {
                    GenGrid(gridPosition);
                }
            }
        }

        private static void WorldMeshGenCallback()
        {
            while (!Exiting)
            {
                WorldMeshGenBarrier.WaitOne();
                WorldMeshGenBarrier.Reset();

                if (GridsToUpdate.IsEmpty)
                {
                    continue;
                }

                while (!Exiting && GridMeshesToUpdate.TryDequeue(out Vector3i gridPosition))
                {
                    if (World.Grids.TryGetValue(gridPosition, out Grid grid))
                    {
                        GenGridMesh(grid);
                    }
                }
            }
        }

        private static void LoadSkyboxShader()
        {
            using var vertexShader = new Shader(ShaderType.VertexShader);
            if (!vertexShader.Compile(File.ReadAllText("./Assets/skybox.vert")))
            {
                Console.WriteLine(vertexShader.GetError());
            }

            using var fragmentShader = new Shader(ShaderType.FragmentShader);
            if (!fragmentShader.Compile(File.ReadAllText("./Assets/skybox.frag")))
            {
                Console.WriteLine(fragmentShader.GetError());
            }

            SkyboxShader = new ShaderProgram();
            SkyboxShader.Attach(vertexShader);
            SkyboxShader.Attach(fragmentShader);
            if (!SkyboxShader.Link())
            {
                Console.WriteLine(SkyboxShader.GetError());
            }
        }

        private static void LoadWorldShader()
        {
            using var vertexShader = new Shader(ShaderType.VertexShader);
            if (!vertexShader.Compile(File.ReadAllText("./Assets/world.vert")))
            {
                Console.WriteLine(vertexShader.GetError());
            }

            using var fragmentShader = new Shader(ShaderType.FragmentShader);
            if (!fragmentShader.Compile(File.ReadAllText("./Assets/world.frag")))
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
        }

        private static void LoadLineShader()
        {
            using var vertexShader = new Shader(ShaderType.VertexShader);
            if (!vertexShader.Compile(File.ReadAllText("./Assets/line.vert")))
            {
                Console.WriteLine(vertexShader.GetError());
            }

            using var fragmentShader = new Shader(ShaderType.FragmentShader);
            if (!fragmentShader.Compile(File.ReadAllText("./Assets/line.frag")))
            {
                Console.WriteLine(fragmentShader.GetError());
            }

            LineShader = new ShaderProgram();
            LineShader.Attach(vertexShader);
            LineShader.Attach(fragmentShader);
            if (!LineShader.Link())
            {
                Console.WriteLine(LineShader.GetError());
            }
        }

        private static void LoadSkybox()
        {
            SkyboxTexture = new TextureCubemap();

            string[] paths = new[] {
                "./Assets/sky_right.png",
                "./Assets/sky_left.png",
                "./Assets/sky_top.png",
                "./Assets/sky_bottom.png",
                "./Assets/sky_front.png",
                "./Assets/sky_back.png",
            };

            Bitmap[] images = paths.Select((string path) =>
            {
                using var image = new Bitmap(path);
                var copy = new Bitmap(image.Width, image.Height, GDIPixelFormat.Format24bppRgb);

                using var graphics = Graphics.FromImage(copy);
                graphics.DrawImage(image, 0, 0, image.Width, image.Height);

                return copy;
            }).ToArray();

            SkyboxTexture.SetImages(images);

            foreach (Bitmap image in images)
            {
                image.Dispose();
            }
        }

        private static void LoadBlocksTexture()
        {
            BlocksTexture = new Texture2D();

            using (var image = new Bitmap("./Assets/blocks.png"))
            using (var copy = new Bitmap(image.Width, image.Height, GDIPixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(copy))
            {
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(image, 0, 0, image.Width, image.Height);

                BlocksTexture.SetImage(copy);
            }
        }

        private static void CreateSkyboxMesh()
        {
            SkyboxMesh = new Mesh<VertexP3>(new VertexP3[] {
                new VertexP3(new Vector3(-1,  1, -1)),
                new VertexP3(new Vector3( 1, -1, -1)),
                new VertexP3(new Vector3(-1, -1, -1)),
                new VertexP3(new Vector3( 1, -1, -1)),
                new VertexP3(new Vector3(-1,  1, -1)),
                new VertexP3(new Vector3( 1,  1, -1)),

                new VertexP3(new Vector3(-1, -1,  1)),
                new VertexP3(new Vector3(-1,  1, -1)),
                new VertexP3(new Vector3(-1, -1, -1)),
                new VertexP3(new Vector3(-1,  1, -1)),
                new VertexP3(new Vector3(-1, -1,  1)),
                new VertexP3(new Vector3(-1,  1,  1)),

                new VertexP3(new Vector3( 1, -1, -1)),
                new VertexP3(new Vector3( 1,  1,  1)),
                new VertexP3(new Vector3( 1, -1,  1)),
                new VertexP3(new Vector3( 1,  1,  1)),
                new VertexP3(new Vector3( 1, -1, -1)),
                new VertexP3(new Vector3( 1,  1, -1)),

                new VertexP3(new Vector3(-1, -1,  1)),
                new VertexP3(new Vector3( 1,  1,  1)),
                new VertexP3(new Vector3(-1,  1,  1)),
                new VertexP3(new Vector3( 1,  1,  1)),
                new VertexP3(new Vector3(-1, -1,  1)),
                new VertexP3(new Vector3( 1, -1,  1)),

                new VertexP3(new Vector3(-1,  1, -1)),
                new VertexP3(new Vector3( 1,  1,  1)),
                new VertexP3(new Vector3( 1,  1, -1)),
                new VertexP3(new Vector3( 1,  1,  1)),
                new VertexP3(new Vector3(-1,  1, -1)),
                new VertexP3(new Vector3(-1,  1,  1)),

                new VertexP3(new Vector3(-1, -1, -1)),
                new VertexP3(new Vector3( 1, -1, -1)),
                new VertexP3(new Vector3(-1, -1,  1)),
                new VertexP3(new Vector3( 1, -1, -1)),
                new VertexP3(new Vector3( 1, -1,  1)),
                new VertexP3(new Vector3(-1, -1,  1)),
            });
            SkyboxMesh.SetVertexAttribs(VertexP3.VertexAttribs);
        }

        private static void CreateLinesMesh()
        {
            LinesMesh = new Mesh<VertexP3C4>(new VertexP3C4[] { });
            LinesMesh.SetVertexAttribs(VertexP3C4.VertexAttribs);
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

            WorldMeshGenThread = new Thread(new ThreadStart(WorldMeshGenCallback));
            WorldMeshGenThread.Start();

            LoadSkyboxShader();
            LoadWorldShader();
            LoadLineShader();

            LoadSkybox();
            LoadBlocksTexture();

            CreateSkyboxMesh();
            CreateLinesMesh();

            GL.Viewport(0, 0, Window.ClientSize.X, Window.ClientSize.Y);
        }

        private static void Window_Resize(ResizeEventArgs obj)
        {
            GL.Viewport(0, 0, Window.ClientSize.X, Window.ClientSize.Y);
        }

        private static void HandlePlayerInput(float deltaTime, bool isOnGround)
        {
            float moveAcceleration = isOnGround ? 25f : 2.5f;

            if (isOnGround && Window.KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                moveAcceleration *= 2;
            }

            var playerMoveDirection = Vector3.Zero;

            if (Window.KeyboardState.IsKeyDown(Keys.A))
            {
                playerMoveDirection -= new Vector3(Camera.Right.X, 0, Camera.Right.Z).Normalized();
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.D))
            {
                playerMoveDirection += new Vector3(Camera.Right.X, 0, Camera.Right.Z).Normalized();
            }

            if (Window.KeyboardState.IsKeyDown(Keys.W))
            {
                playerMoveDirection += new Vector3(Camera.Front.X, 0, Camera.Front.Z).Normalized();
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.S))
            {
                playerMoveDirection -= new Vector3(Camera.Front.X, 0, Camera.Front.Z).Normalized();
            }

            if (playerMoveDirection.LengthSquared > 10e-3f)
            {
                Player.Velocity += playerMoveDirection.Normalized() * deltaTime * moveAcceleration;
            }

            if (isOnGround && Window.KeyboardState.IsKeyDown(Keys.Space) && LastJumpTime < CurrentTime - 0.5f)
            {
                const float jumpAcceleration = 380f;

                Player.Velocity += Vector3.UnitY * deltaTime * jumpAcceleration;
                LastJumpTime = CurrentTime;
            }

            if (Window.KeyboardState.IsKeyDown(Keys.Left))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, deltaTime) * Camera.Rotation;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.Right))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, -deltaTime) * Camera.Rotation;
            }

            if (Window.KeyboardState.IsKeyDown(Keys.Up))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Camera.Right, deltaTime) * Camera.Rotation;
            }
            else if (Window.KeyboardState.IsKeyDown(Keys.Down))
            {
                Camera.Rotation = Quaternion.FromAxisAngle(Camera.Right, -deltaTime) * Camera.Rotation;
            }

            Camera.Rotation.Normalize();
        }

        private static void Window_UpdateFrame(FrameEventArgs obj)
        {
            const float g = 9.81f;
            const float frictionFactor = 0.9f;
            const float airFrictionFactor = 0.99f;
            const float maxPenetration = 0.25f;

            var halfSize = 0.5f * Player.Size;
            bool isOnGround = false;

            var leftVertexes = new[] {
                new Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z),
                new Vector3(-halfSize.X,           0, -halfSize.Z),
                new Vector3(-halfSize.X,  halfSize.Y, -halfSize.Z),
                new Vector3(-halfSize.X, -halfSize.Y,  halfSize.Z),
                new Vector3(-halfSize.X,           0,  halfSize.Z),
                new Vector3(-halfSize.X,  halfSize.Y,  halfSize.Z),
            };

            foreach (Vector3 vertexPosition in leftVertexes)
            {
                float maxDistance = (float)obj.Time * Math.Abs(Player.Velocity.X);
                var ray = new Ray(Player.Position + vertexPosition, -Vector3.UnitX);
                if (World.RaycastBlock(ray, 0f, Math.Max(WorldGen.GRID_SIZE, maxDistance), out Vector3 blockIntersection, out _).HasValue)
                {
                    if (blockIntersection.X >= Player.Position.X - halfSize.X + (float)obj.Time * Player.Velocity.X
                        && blockIntersection.X < Player.Position.X - halfSize.X + (float)obj.Time * Player.Velocity.X + maxPenetration)
                    {
                        Player.Position.X = blockIntersection.X + halfSize.X;
                        Player.Velocity.X = 0;
                        break;
                    }
                }
            }

            var rightVertexes = new[] {
                new Vector3(halfSize.X, -halfSize.Y, -halfSize.Z),
                new Vector3(halfSize.X,           0, -halfSize.Z),
                new Vector3(halfSize.X,  halfSize.Y, -halfSize.Z),
                new Vector3(halfSize.X, -halfSize.Y,  halfSize.Z),
                new Vector3(halfSize.X,           0,  halfSize.Z),
                new Vector3(halfSize.X,  halfSize.Y,  halfSize.Z),
            };

            foreach (Vector3 vertexPosition in rightVertexes)
            {
                float maxDistance = (float)obj.Time * Math.Abs(Player.Velocity.X);
                var ray = new Ray(Player.Position + vertexPosition, Vector3.UnitX);
                if (World.RaycastBlock(ray, 0f, Math.Max(WorldGen.GRID_SIZE, maxDistance), out Vector3 blockIntersection, out _).HasValue)
                {
                    if (blockIntersection.X <= Player.Position.X + halfSize.X + (float)obj.Time * Player.Velocity.X
                        && blockIntersection.X > Player.Position.X + halfSize.X + (float)obj.Time * Player.Velocity.X - maxPenetration)
                    {
                        Player.Position.X = blockIntersection.X - halfSize.X;
                        Player.Velocity.X = 0;
                        break;
                    }
                }
            }

            var frontVertexes = new[] {
                new Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z),
                new Vector3(-halfSize.X,           0, -halfSize.Z),
                new Vector3(-halfSize.X,  halfSize.Y, -halfSize.Z),
                new Vector3( halfSize.X, -halfSize.Y, -halfSize.Z),
                new Vector3( halfSize.X,           0, -halfSize.Z),
                new Vector3( halfSize.X,  halfSize.Y, -halfSize.Z),
            };

            foreach (Vector3 vertexPosition in frontVertexes)
            {
                float maxDistance = (float)obj.Time * Math.Abs(Player.Velocity.Z);
                var ray = new Ray(Player.Position + vertexPosition, -Vector3.UnitZ);
                if (World.RaycastBlock(ray, 0f, Math.Max(WorldGen.GRID_SIZE, maxDistance), out Vector3 blockIntersection, out _).HasValue)
                {
                    if (blockIntersection.Z >= Player.Position.Z - halfSize.Z + (float)obj.Time * Player.Velocity.Z
                        && blockIntersection.Z < Player.Position.Z - halfSize.Z + (float)obj.Time * Player.Velocity.Z + maxPenetration)
                    {
                        Player.Position.Z = blockIntersection.Z + halfSize.Z;
                        Player.Velocity.Z = 0;
                        break;
                    }
                }
            }

            var backVertexes = new[] {
                new Vector3(-halfSize.X, -halfSize.Y, halfSize.Z),
                new Vector3(-halfSize.X,           0, halfSize.Z),
                new Vector3(-halfSize.X,  halfSize.Y, halfSize.Z),
                new Vector3( halfSize.X, -halfSize.Y, halfSize.Z),
                new Vector3( halfSize.X,           0, halfSize.Z),
                new Vector3( halfSize.X,  halfSize.Y, halfSize.Z),
            };

            foreach (Vector3 vertexPosition in backVertexes)
            {
                float maxDistance = (float)obj.Time * Math.Abs(Player.Velocity.Z);
                var ray = new Ray(Player.Position + vertexPosition, Vector3.UnitZ);
                if (World.RaycastBlock(ray, 0f, Math.Max(WorldGen.GRID_SIZE, maxDistance), out Vector3 blockIntersection, out _).HasValue)
                {
                    if (blockIntersection.Z <= Player.Position.Z + halfSize.Z + (float)obj.Time * Player.Velocity.Z
                        && blockIntersection.Z > Player.Position.Z + halfSize.Z + (float)obj.Time * Player.Velocity.Z - maxPenetration)
                    {
                        Player.Position.Z = blockIntersection.Z - halfSize.Z;
                        Player.Velocity.Z = 0;
                        break;
                    }
                }
            }

            var bottomVertexes = new[] {
                new Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z),
                new Vector3( halfSize.X, -halfSize.Y, -halfSize.Z),
                new Vector3( halfSize.X, -halfSize.Y,  halfSize.Z),
                new Vector3(-halfSize.X, -halfSize.Y,  halfSize.Z),
            };

            foreach (Vector3 vertexPosition in bottomVertexes)
            {
                float maxDistance = (float)obj.Time * Math.Abs(Player.Velocity.Y);
                var ray = new Ray(Player.Position + vertexPosition, -Vector3.UnitY);
                if (World.RaycastBlock(ray, 0f, Math.Max(WorldGen.GRID_SIZE, maxDistance), out Vector3 blockIntersection, out _).HasValue)
                {
                    if (blockIntersection.Y >= Player.Position.Y - halfSize.Y + (float)obj.Time * Player.Velocity.Y
                        && blockIntersection.Y < Player.Position.Y - halfSize.Y + (float)obj.Time * Player.Velocity.Y + maxPenetration)
                    {
                        Player.Position.Y = blockIntersection.Y + halfSize.Y;
                        Player.Velocity.Y = 0;

                        isOnGround = true;
                        break;
                    }
                }
            }

            var topVertexes = new[] {
                new Vector3(-halfSize.X, halfSize.Y, -halfSize.Z),
                new Vector3( halfSize.X, halfSize.Y, -halfSize.Z),
                new Vector3( halfSize.X, halfSize.Y,  halfSize.Z),
                new Vector3(-halfSize.X, halfSize.Y,  halfSize.Z),
            };

            foreach (Vector3 vertexPosition in topVertexes)
            {
                float maxDistance = (float)obj.Time * Math.Abs(Player.Velocity.Y);
                var ray = new Ray(Player.Position + vertexPosition, Vector3.UnitY);
                if (World.RaycastBlock(ray, 0f, Math.Max(WorldGen.GRID_SIZE, maxDistance), out Vector3 blockIntersection, out _).HasValue)
                {
                    if (blockIntersection.Y <= Player.Position.Y + halfSize.Y + (float)obj.Time * Player.Velocity.Y
                        && blockIntersection.Y > Player.Position.Y + halfSize.Y + (float)obj.Time * Player.Velocity.Y - maxPenetration)
                    {
                        Player.Position.Y = blockIntersection.Y - halfSize.Y;
                        Player.Velocity.Y = 0;
                        break;
                    }
                }
            }

            Player.Position += (float)obj.Time * Player.Velocity;
            Camera.Position = Player.Position + 0.5f * Vector3.UnitY;

            HandlePlayerInput((float)obj.Time, isOnGround);

            Player.Velocity -= g * (float)obj.Time * Vector3.UnitY;
            Player.Velocity *= isOnGround ? frictionFactor : airFrictionFactor;

            if (Window.RenderTime > 0)
            {
                float fps = (float)(PreviousFPS * 0.9f + 1f / Window.RenderTime * 0.1f);
                PreviousFPS = fps;
            }

            Window.Title = $"Cubach - {PreviousFPS:N2} FPS";

            CurrentTime += (float)obj.Time;
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

        private static readonly ConcurrentQueue<Action> DeferredActions = new ConcurrentQueue<Action>();

        private static void GenGridMesh(Grid grid)
        {
            if (!WorldOpaqueMeshes.ContainsKey(grid.Position))
            {
                VertexP3N3T2[] vertexes = grid.GenVertexes();
                if (IsMainThread)
                {
                    if (WorldOpaqueMeshes.TryRemove(grid.Position, out Mesh<VertexP3N3T2> existingMesh))
                    {
                        existingMesh.Dispose();
                    }

                    var mesh = new Mesh<VertexP3N3T2>(vertexes);
                    mesh.SetVertexAttribs(VertexP3N3T2.VertexAttribs);
                    if (!WorldOpaqueMeshes.TryAdd(grid.Position, mesh))
                    {
                        mesh.Dispose();
                    }
                }
                else
                {
                    DeferredActions.Enqueue(() =>
                    {
                        if (WorldOpaqueMeshes.TryRemove(grid.Position, out Mesh<VertexP3N3T2> existingMesh))
                        {
                            existingMesh.Dispose();
                        }

                        var mesh = new Mesh<VertexP3N3T2>(vertexes);
                        mesh.SetVertexAttribs(VertexP3N3T2.VertexAttribs);
                        if (!WorldOpaqueMeshes.TryAdd(grid.Position, mesh))
                        {
                            mesh.Dispose();
                        }
                    });
                }

                Console.WriteLine($"Generated opaque grid mesh {grid.Position}");
            }

            if (!WorldTransparentMeshes.ContainsKey(grid.Position))
            {
                VertexP3N3T2[] vertexes = grid.GenVertexes(opaque: false);
                if (IsMainThread)
                {
                    if (WorldTransparentMeshes.TryRemove(grid.Position, out Mesh<VertexP3N3T2> existingMesh))
                    {
                        existingMesh.Dispose();
                    }

                    var mesh = new Mesh<VertexP3N3T2>(vertexes);
                    mesh.SetVertexAttribs(VertexP3N3T2.VertexAttribs);
                    if (!WorldTransparentMeshes.TryAdd(grid.Position, mesh))
                    {
                        mesh.Dispose();
                    }
                }
                else
                {
                    DeferredActions.Enqueue(() =>
                    {
                        if (WorldTransparentMeshes.TryRemove(grid.Position, out Mesh<VertexP3N3T2> existingMesh))
                        {
                            existingMesh.Dispose();
                        }

                        var mesh = new Mesh<VertexP3N3T2>(vertexes);
                        mesh.SetVertexAttribs(VertexP3N3T2.VertexAttribs);
                        if (!WorldTransparentMeshes.TryAdd(grid.Position, mesh))
                        {
                            mesh.Dispose();
                        }
                    });
                }

                Console.WriteLine($"Generated transparent grid mesh {grid.Position}");
            }
        }

        private static void UnloadGridMesh(Vector3i gridPosition)
        {
            if (WorldOpaqueMeshes.TryRemove(gridPosition, out Mesh<VertexP3N3T2> opaqueMesh))
            {
                opaqueMesh.Dispose();
                Console.WriteLine($"Unloaded opaque grid mesh {gridPosition}");
            }

            if (WorldTransparentMeshes.TryRemove(gridPosition, out Mesh<VertexP3N3T2> transparentMesh))
            {
                transparentMesh.Dispose();
                Console.WriteLine($"Unloaded transparent grid mesh {gridPosition}");
            }
        }

        private static void GenNearGrids()
        {
            const int minHeight = -2;
            const int maxHeight = 6;

            bool added = false;
            Vector3i cameraPosition = (Vector3i)(Camera.Position / WorldGen.GRID_SIZE);
            int genDistance = (int)MathF.Floor(GRID_GEN_DISTANCE / WorldGen.GRID_SIZE);
            var gridsToUpdate = new List<Vector3i>();

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

                        gridsToUpdate.Add(gridPosition);
                        added = true;
                    }
                }
            }

            if (!added)
            {
                return;
            }

            foreach (Vector3i gridPosition in gridsToUpdate)
            {
                GridsToUpdate.Enqueue(gridPosition);
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
            bool found = false;
            var gridMeshesToUpdate = new List<Vector3i>();
            foreach (Vector3i gridPosition in World.Grids.Keys)
            {
                if (WorldOpaqueMeshes.ContainsKey(gridPosition) || WorldTransparentMeshes.ContainsKey(gridPosition))
                {
                    continue;
                }

                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance < MESH_GEN_DISTANCE)
                {
                    gridMeshesToUpdate.Add(gridPosition);
                    found = true;
                }
            }

            if (!found)
            {
                return;
            }

            gridMeshesToUpdate.Sort((Vector3i a, Vector3i b) =>
            {
                Vector3 aGridCenter = GetGridCenter(a);
                Vector3 bGridCenter = GetGridCenter(b);

                float aDistance = Coords.TaxicabDistance3(aGridCenter, Camera.Position);
                float bDistance = Coords.TaxicabDistance3(bGridCenter, Camera.Position);

                return MathF.Sign(aDistance - bDistance);
            });

            foreach (Vector3i gridPosition in gridMeshesToUpdate)
            {
                if (World.Grids.ContainsKey(gridPosition))
                {
                    GridMeshesToUpdate.Enqueue(gridPosition);
                }
            }

            if (!GridMeshesToUpdate.IsEmpty)
            {
                WorldMeshGenBarrier.Set();
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

        private static void DrawSkybox(ref Matrix4 view, ref Matrix4 projection)
        {
            GL.DepthMask(false);

            SkyboxShader.Use();

            int skyboxTextureLocation = SkyboxShader.GetUniformLocation("skyboxTexture");
            int vpLocation = SkyboxShader.GetUniformLocation("vp");

            GL.Uniform1(skyboxTextureLocation, 0);

            Matrix3 v3 = new Matrix3(view);
            Matrix4 v4 = new Matrix4(v3);
            Matrix4 vp = v4 * projection;
            GL.UniformMatrix4(vpLocation, false, ref vp);

            SkyboxMesh.Draw();

            GL.DepthMask(true);
        }

        private static void DrawWorld(ref Matrix4 view, ref Matrix4 projection)
        {
            WorldShader.Use();

            GL.UniformMatrix4(WorldShader.GetUniformLocation("viewMatrix"), false, ref view);
            GL.UniformMatrix4(WorldShader.GetUniformLocation("projectionMatrix"), false, ref projection);

            GL.Uniform1(WorldShader.GetUniformLocation("colorTexture"), 0);
            GL.Uniform3(WorldShader.GetUniformLocation("light"), new Vector3(0.2f, 1, 0.1f).Normalized());

            BlocksTexture.Bind();

            foreach ((Vector3i gridPosition, var mesh) in WorldOpaqueMeshes)
            {
                Vector3 gridCenter = GetGridCenter(gridPosition);
                float distance = Coords.TaxicabDistance3(gridCenter, Camera.Position);
                if (distance > MAX_RENDER_DISTANCE)
                {
                    continue;
                }

                Matrix4 model = Matrix4.CreateTranslation(16 * gridPosition);
                GL.UniformMatrix4(WorldShader.GetUniformLocation("modelMatrix"), false, ref model);
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
                Matrix4 model = Matrix4.CreateTranslation(16 * gridPosition);
                GL.UniformMatrix4(WorldShader.GetUniformLocation("modelMatrix"), false, ref model);
                mesh.Draw();
            }
        }

        private static void DrawLines(ref Matrix4 viewProjection, VertexP3C4[] vertexes)
        {
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.CullFace);

            LineShader.Use();

            int lineMvpLocation = LineShader.GetUniformLocation("mvp");
            GL.UniformMatrix4(lineMvpLocation, false, ref viewProjection);

            LinesMesh.SetData(vertexes, BufferUsageHint.DynamicDraw);
            LinesMesh.Draw();

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
        }

        private static void Window_RenderFrame(FrameEventArgs obj)
        {
            while (DeferredActions.TryDequeue(out Action action))
            {
                action();
            }

            GenNearGrids();
            UnloadFarGrids();

            GenNearGridMeshes();
            UnloadFarGridMeshes();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 view = Camera.ViewMatrix;
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)Window.ClientSize.X / Window.ClientSize.Y, 0.1f, 512f);
            Matrix4 viewProjection = view * projection;

            DrawSkybox(ref view, ref projection);
            DrawWorld(ref view, ref projection);

            var lineVertexes = new List<VertexP3C4>();
            var ray = new Ray(Camera.Position, Camera.Front);
            Block? block = World.RaycastBlock(ray, 0, 128, out _, out Vector3i blockPosition);
            if (block.HasValue)
            {
                const float selectionSize = 1.01f;
                const float lineWidth = 0.01f;

                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(0, 0, 0), blockPosition + selectionSize * new Vector3(1, 0, 0), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(1, 0, 0), blockPosition + selectionSize * new Vector3(1, 1, 0), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(1, 1, 0), blockPosition + selectionSize * new Vector3(0, 1, 0), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(0, 1, 0), blockPosition + selectionSize * new Vector3(0, 0, 0), Color4.LightGray, lineWidth));

                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(0, 0, 1), blockPosition + selectionSize * new Vector3(1, 0, 1), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(1, 0, 1), blockPosition + selectionSize * new Vector3(1, 1, 1), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(1, 1, 1), blockPosition + selectionSize * new Vector3(0, 1, 1), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(0, 1, 1), blockPosition + selectionSize * new Vector3(0, 0, 1), Color4.LightGray, lineWidth));

                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(0, 0, 0), blockPosition + selectionSize * new Vector3(0, 0, 1), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(1, 0, 0), blockPosition + selectionSize * new Vector3(1, 0, 1), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(1, 1, 0), blockPosition + selectionSize * new Vector3(1, 1, 1), Color4.LightGray, lineWidth));
                lineVertexes.AddRange(GenLineVertexes(blockPosition + selectionSize * new Vector3(0, 1, 0), blockPosition + selectionSize * new Vector3(0, 1, 1), Color4.LightGray, lineWidth));
            }

            if (lineVertexes.Count > 0)
            {
                DrawLines(ref viewProjection, lineVertexes.ToArray());
            }

            Window.SwapBuffers();
        }

        private static void Window_Closed()
        {
            SkyboxShader.Dispose();
            WorldShader.Dispose();
            LineShader.Dispose();

            SkyboxTexture.Dispose();
            BlocksTexture.Dispose();

            SkyboxMesh.Dispose();
            LinesMesh.Dispose();

            foreach (var mesh in WorldOpaqueMeshes.Values)
            {
                mesh.Dispose();
            }

            foreach (var mesh in WorldTransparentMeshes.Values)
            {
                mesh.Dispose();
            }
        }
    }
}
