using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Cubach.Client
{
    public sealed class WorldGen
    {
        public const int GridSize = 16;

        public readonly Dictionary<Vector2i, int> HeightMap = new Dictionary<Vector2i, int>();

        public Grid GenGrid(World world, Vector3i gridPosition)
        {
            var grid = new Grid(world, gridPosition, new Vector3i(GridSize));
            for (int i = 0; i < grid.Width; ++i)
            {
                for (int k = 0; k < grid.Length; ++k)
                {
                    int x = GridSize * gridPosition.X + i;
                    int z = GridSize * gridPosition.Z + k;

                    var position = new Vector2i(x, z);
                    if (!HeightMap.TryGetValue(position, out int maxHeight))
                    {
                        float noise = 4 * (MultiOctaveNoise.Gen2(new Vector2(x, z)) + 1) / 2;
                        maxHeight = GridSize + (int)Math.Floor(GridSize * noise);
                    }

                    for (int j = 0; j < grid.Height; ++j)
                    {
                        int y = GridSize * gridPosition.Y + j;

                        if (y < maxHeight)
                        {
                            grid.Blocks[i, j, k] = new Block(1);
                        }
                        else if (y == maxHeight)
                        {
                            grid.Blocks[i, j, k] = new Block(2);
                        }
                        else
                        {
                            grid.Blocks[i, j, k] = new Block(0);
                        }
                    }
                }
            }

            grid.UpdateIsEmpty();

            return grid;
        }
    }
}
