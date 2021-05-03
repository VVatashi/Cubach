using System.Collections.Generic;

namespace Cubach.Client
{
    public class BlockType
    {
        public bool GenGeometry;
        public int TextureId;
        public int TopTextureId;
        public int BottomTextureId;

        public static Dictionary<byte, BlockType> Types = new Dictionary<byte, BlockType>
        {
            [0] = new BlockType { GenGeometry = false, TextureId = 0, TopTextureId = 0, BottomTextureId = 0 },
            [1] = new BlockType { GenGeometry = true, TextureId = 1, TopTextureId = 1, BottomTextureId = 1 },
            [2] = new BlockType { GenGeometry = true, TextureId = 2, TopTextureId = 0, BottomTextureId = 1 },
        };
    }
}
