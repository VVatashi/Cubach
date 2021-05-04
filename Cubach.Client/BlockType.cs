using System.Collections.Generic;

namespace Cubach.Client
{
    public class BlockType
    {
        public string Name;
        public bool GenGeometry;
        public int TextureId;
        public int TopTextureId;
        public int BottomTextureId;
        public bool Opaque;

        public static Dictionary<byte, BlockType> Types = new Dictionary<byte, BlockType>
        {
            [0] = new BlockType("air", 0, false, false),
            [1] = new BlockType("dirt", 1),
            [2] = new BlockType("grass", 2, 0, 1),
            [3] = new BlockType("stone", 3),
            [4] = new BlockType("sand", 4),
            [5] = new BlockType("wood", 5, 6, 6),
            [6] = new BlockType("leaves", 7, false),
            [7] = new BlockType("glass", 8, false),
            [8] = new BlockType("water", 9, false),
        };

        public BlockType(string name, int textureId, int topTextureId, int bottomTextureId, bool opaque = true, bool genGeometry = true)
        {
            Name = name;
            GenGeometry = genGeometry;
            TextureId = textureId;
            TopTextureId = topTextureId;
            BottomTextureId = bottomTextureId;
            Opaque = opaque;
        }

        public BlockType(string name, int textureId, bool opaque = true, bool genGeometry = true) : this(name, textureId, textureId, textureId, opaque, genGeometry) { }

        public static BlockType GetByName(string name)
        {
            foreach (BlockType blockType in Types.Values)
            {
                if (blockType.Name == name)
                {
                    return blockType;
                }
            }

            return null;
        }

        public static byte? GetIdByName(string name)
        {
            foreach ((byte id, BlockType blockType) in Types)
            {
                if (blockType.Name == name)
                {
                    return id;
                }
            }

            return null;
        }
    }
}
