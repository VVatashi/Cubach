using OpenTK.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

        public Grid GenGrid(Vector3i gridPosition)
        {
            Grid grid = WorldGen.GenGrid(this, gridPosition);
            Grids.TryAdd(gridPosition, grid);

            return grid;
        }

        public Block? GetBlockAt(Vector3i position)
        {
            int gridX = (int)MathF.Floor((float)position.X / WorldGen.GridSize);
            int gridY = (int)MathF.Floor((float)position.Y / WorldGen.GridSize);
            int gridZ = (int)MathF.Floor((float)position.Z / WorldGen.GridSize);

            var gridPosition = new Vector3i(gridX, gridY, gridZ);
            if (!Grids.ContainsKey(gridPosition))
            {
                return null;
            }

            Grid grid = Grids[gridPosition];

            int blockX = position.X - gridX * WorldGen.GridSize;
            int blockY = position.Y - gridY * WorldGen.GridSize;
            int blockZ = position.Z - gridZ * WorldGen.GridSize;

            return grid.GetBlockAt(new Vector3i(blockX, blockY, blockZ));
        }

        public BlockType GetBlockTypeAt(Vector3i position)
        {
            Block? block = GetBlockAt(position);

            return block.HasValue ? block.Value.Type : null;
        }
    }
}
