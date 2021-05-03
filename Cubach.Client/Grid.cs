using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Cubach.Client
{
    public sealed class Grid
    {
        public readonly World World;
        public readonly Block[,,] Blocks;
        public readonly Vector3i Position;
        public readonly Vector3i Size;

        public Grid(World world, Vector3i position, Vector3i size)
        {
            World = world;
            Blocks = new Block[size.X, size.Y, size.Z];
            Position = position;
            Size = size;
        }

        public int Width => Size.X;
        public int Height => Size.Y;
        public int Length => Size.Z;

        public VertexP3N3T2[] GenVertexes()
        {
            var result = new List<VertexP3N3T2>(Blocks.Length * Block.VertexCount);

            for (int i = 0; i < Width; ++i)
            {
                for (int j = 0; j < Height; ++j)
                {
                    for (int k = 0; k < Length; ++k)
                    {
                        Vector3i blockPosition = 16 * Position + new Vector3i(i, j, k);

                        BlockType leftBlockType = World.GetBlockTypeAt(blockPosition - Vector3i.UnitX);
                        BlockType rightBlockType = World.GetBlockTypeAt(blockPosition + Vector3i.UnitX);

                        BlockType bottomBlockType = World.GetBlockTypeAt(blockPosition - Vector3i.UnitY);
                        BlockType topBlockType = World.GetBlockTypeAt(blockPosition + Vector3i.UnitY);

                        BlockType frontBlockType = World.GetBlockTypeAt(blockPosition - Vector3i.UnitZ);
                        BlockType backBlockType = World.GetBlockTypeAt(blockPosition + Vector3i.UnitZ);

                        bool hasLeftBlock = leftBlockType != null && leftBlockType.GenGeometry;
                        bool hasRightBlock = rightBlockType != null && rightBlockType.GenGeometry;

                        bool hasBottomBlock = bottomBlockType != null && bottomBlockType.GenGeometry;
                        bool hasTopBlock = topBlockType != null && topBlockType.GenGeometry;

                        bool hasFrontBlock = frontBlockType != null && frontBlockType.GenGeometry;
                        bool hasBackBlock = backBlockType != null && backBlockType.GenGeometry;

                        if (hasLeftBlock && hasRightBlock && hasBottomBlock && hasTopBlock && hasFrontBlock && hasBackBlock)
                        {
                            continue;
                        }

                        Block block = Blocks[i, j, k];
                        VertexP3N3T2[] blockVertexes = block.GenVertexes(i, j, k);
                        result.AddRange(blockVertexes);
                    }
                }
            }

            return result.ToArray();
        }

        public Block? GetBlockAt(Vector3i position)
        {
            if (position.X < 0 || position.Y < 0 || position.Z < 0 || position.X > Width || position.Y > Height || position.Z > Length)
            {
                return null;
            }

            return Blocks[position.X, position.Y, position.Z];
        }

        public BlockType GetBlockTypeAt(Vector3i position)
        {
            Block? block = GetBlockAt(position);

            return block.HasValue ? block.Value.Type : null;
        }
    }
}
