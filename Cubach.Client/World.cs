using OpenTK.Mathematics;
using System;
using System.Collections.Concurrent;

namespace Cubach.Client
{
    public sealed class World
    {
        public readonly WorldGen WorldGen;

        public readonly ConcurrentDictionary<Vector3i, Grid> Grids = new ConcurrentDictionary<Vector3i, Grid>();

        public World(WorldGen worldGen)
        {
            WorldGen = worldGen;
        }

        public Grid GenGridAt(Vector3i gridPosition)
        {
            Grid grid = WorldGen.GenGrid(this, gridPosition);
            Grids.TryAdd(gridPosition, grid);

            return grid;
        }

        public Grid GetGridAt(Vector3i gridPosition)
        {
            if (Grids.TryGetValue(gridPosition, out Grid grid))
            {
                return grid;
            }

            return null;
        }

        public Block? GetBlockAt(Vector3i position)
        {
            int gridX = (int)MathF.Floor((float)position.X / WorldGen.GRID_SIZE);
            int gridY = (int)MathF.Floor((float)position.Y / WorldGen.GRID_SIZE);
            int gridZ = (int)MathF.Floor((float)position.Z / WorldGen.GRID_SIZE);

            var gridPosition = new Vector3i(gridX, gridY, gridZ);
            if (!Grids.ContainsKey(gridPosition))
            {
                return null;
            }

            Grid grid = Grids[gridPosition];

            int blockX = position.X - gridX * WorldGen.GRID_SIZE;
            int blockY = position.Y - gridY * WorldGen.GRID_SIZE;
            int blockZ = position.Z - gridZ * WorldGen.GRID_SIZE;

            return grid.GetBlockAt(new Vector3i(blockX, blockY, blockZ));
        }

        public BlockType GetBlockTypeAt(Vector3i position)
        {
            Block? block = GetBlockAt(position);

            return block.HasValue ? block.Value.Type : null;
        }

        public void SetBlockAt(Vector3i position, Block block, bool update = true)
        {
            int gridX = (int)MathF.Floor((float)position.X / WorldGen.GRID_SIZE);
            int gridY = (int)MathF.Floor((float)position.Y / WorldGen.GRID_SIZE);
            int gridZ = (int)MathF.Floor((float)position.Z / WorldGen.GRID_SIZE);

            var gridPosition = new Vector3i(gridX, gridY, gridZ);
            if (!Grids.ContainsKey(gridPosition))
            {
                GenGridAt(gridPosition);
            }

            Grid grid = Grids[gridPosition];

            int blockX = position.X - gridX * WorldGen.GRID_SIZE;
            int blockY = position.Y - gridY * WorldGen.GRID_SIZE;
            int blockZ = position.Z - gridZ * WorldGen.GRID_SIZE;

            grid.Blocks[blockX, blockY, blockZ] = block;

            if (update)
            {
                grid.UpdateIsEmpty();
            }
        }

        public Grid RaycastGrid(Ray ray, float minDistance, float maxDistance, out Vector3 intersection)
        {
            float distance = minDistance;
            while (distance < maxDistance)
            {
                Vector3 position = ray.Origin + ray.Direction * distance;
                Vector3i gridPosition = new Vector3i((int)Math.Floor(position.X / 16), (int)Math.Floor(position.Y / 16), (int)Math.Floor(position.Z / 16));
                Grid grid = GetGridAt(gridPosition);
                if (grid == null)
                {
                    break;
                }

                AABB gridAABB = new AABB(16 * grid.Position, 16 * grid.Position + grid.Size);
                if (!CollisionDetection.RayAABBIntersection3(gridAABB, ray, out Vector3 nearIntersection, out Vector3 farIntersection))
                {
                    break;
                }

                if (!grid.IsEmpty)
                {
                    intersection = farIntersection;
                    return grid;
                }

                distance = (farIntersection - ray.Origin).Length + 10e-3f;
            }

            intersection = Vector3.Zero;
            return null;
        }
    }
}
