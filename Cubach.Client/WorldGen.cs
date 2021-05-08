using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Cubach.Client
{
    public sealed class WorldGen
    {
        public const int SEA_LEVEL = 28;
        public const int GRID_SIZE = 16;

        public const int BASE_HEIGHT = 16;
        public const float HEIGHT_AMPLITUDE = 16f;
        public const float HEIGHT_POW = 1.5f;

        public readonly Dictionary<Vector2i, int> HeightMap = new Dictionary<Vector2i, int>();
        public readonly Dictionary<Vector2i, float> TreeMap = new Dictionary<Vector2i, float>();

        public int GetHeightAt(Vector2i position)
        {
            if (HeightMap.TryGetValue(position, out int height))
            {
                return height;
            }

            float noise = HEIGHT_AMPLITUDE * (MultiOctaveNoise.Gen2(position) + 1) / 2;
            noise = MathF.Pow(noise, HEIGHT_POW);

            return HeightMap[position] = BASE_HEIGHT + (int)Math.Floor(noise);
        }

        public float GetTreeAt(Vector2i position)
        {
            if (TreeMap.TryGetValue(position, out float probability))
            {
                return probability;
            }

            return TreeMap[position] = PerlinNoise.Gen2((Vector2)position / 16f);
        }

        public Grid GenGrid(World world, Vector3i gridPosition)
        {
            byte airId = (byte)BlockType.GetIdByName("air");
            byte grassId = (byte)BlockType.GetIdByName("grass");
            byte sandId = (byte)BlockType.GetIdByName("sand");
            byte dirtId = (byte)BlockType.GetIdByName("dirt");
            byte stoneId = (byte)BlockType.GetIdByName("stone");
            byte waterId = (byte)BlockType.GetIdByName("water");

            var grid = new Grid(world, gridPosition, new Vector3i(GRID_SIZE));
            world.Grids[gridPosition] = grid;

            for (int i = 0; i < grid.Width; ++i)
            {
                for (int k = 0; k < grid.Length; ++k)
                {
                    int x = GRID_SIZE * gridPosition.X + i;
                    int z = GRID_SIZE * gridPosition.Z + k;

                    var position = new Vector2i(x, z);
                    int height = GetHeightAt(position);
                    for (int j = 0; j < grid.Height; ++j)
                    {
                        int y = GRID_SIZE * gridPosition.Y + j;

                        if (y < height - 8)
                        {
                            grid.Blocks[i, j, k] = new Block(stoneId);
                        }
                        else if (y < height)
                        {
                            grid.Blocks[i, j, k] = y <= SEA_LEVEL ? new Block(sandId) : new Block(dirtId);
                        }
                        else if (y == height)
                        {
                            grid.Blocks[i, j, k] = y <= SEA_LEVEL ? new Block(sandId) : new Block(grassId);
                        }
                        else
                        {
                            grid.Blocks[i, j, k] = y < SEA_LEVEL ? new Block(waterId) : new Block(airId);
                        }
                    }
                }
            }

            GenTrees(world, gridPosition);

            grid.UpdateIsEmpty();

            return grid;
        }

        public void GenTrees(World world, Vector3i gridPosition)
        {
            const int treeHeight = 5;

            var airId = (byte)BlockType.GetIdByName("air");
            var woodId = (byte)BlockType.GetIdByName("wood");
            var leavesId = (byte)BlockType.GetIdByName("leaves");

            var grid = world.GetGridAt(gridPosition);
            if (grid == null)
            {
                return;
            }

            for (int i = 0; i < grid.Width; ++i)
            {
                for (int k = 0; k < grid.Length; ++k)
                {
                    int x = GRID_SIZE * gridPosition.X + i;
                    int z = GRID_SIZE * gridPosition.Z + k;

                    var position = new Vector2i(x, z);
                    int height = GetHeightAt(position);
                    if (height < GRID_SIZE * gridPosition.Y || height > GRID_SIZE * (gridPosition.Y + 1) || height <= SEA_LEVEL)
                    {
                        continue;
                    }

                    float probability = GetTreeAt(position);
                    bool placeTree = probability > GetTreeAt(position - Vector2i.UnitX) && probability > GetTreeAt(position + Vector2i.UnitX)
                        && probability > GetTreeAt(position - Vector2i.UnitY) && probability > GetTreeAt(position + Vector2i.UnitY)
                        && probability > GetTreeAt(position - Vector2i.One) && probability > GetTreeAt(position + Vector2i.One);

                    if (placeTree)
                    {
                        for (int j = 0; j < treeHeight; ++j)
                        {
                            Vector3i blockPosition = new Vector3i(x, height + j, z);
                            Block? block = world.GetBlockAt(blockPosition);
                            if (!block.HasValue || block.Value.BlockTypeId == airId)
                            {
                                world.SetBlockAt(blockPosition, new Block(woodId));
                            }
                        }

                        for (int ii = -2; ii <= 2; ++ii)
                        {
                            for (int kk = -2; kk <= 2; ++kk)
                            {
                                int leavesMinHeight = ii == 0 && kk == 0 ? treeHeight : 3;
                                int leavesMaxHeight = Math.Abs(ii) == 2 || Math.Abs(kk) == 2
                                    ? treeHeight
                                    : Math.Abs(ii) == 0 || Math.Abs(kk) == 0
                                        ? treeHeight + 2
                                        : treeHeight + 1;

                                for (int j = leavesMinHeight; j < leavesMaxHeight; ++j)
                                {
                                    Vector3i blockPosition = new Vector3i(x + ii, height + j, z + kk);
                                    Block? block = world.GetBlockAt(blockPosition);
                                    if (!block.HasValue || block.Value.BlockTypeId == airId)
                                    {
                                        world.SetBlockAt(blockPosition, new Block(leavesId));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
