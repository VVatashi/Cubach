using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Cubach.Client
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Block
    {
        public const int VertexCount = 36;

        public byte BlockTypeId;

        public BlockType Type
        {
            get => BlockType.Types[BlockTypeId];
        }

        public Block(byte blockTypeId)
        {
            BlockTypeId = blockTypeId;
        }

        public VertexP3N3T2[] GenLeftVertexes(int x, int y, int z)
        {
            if (!Type.GenGeometry)
            {
                return new VertexP3N3T2[0];
            }

            float u1 = (Type.TextureId % 16) / 16f;
            float u2 = (Type.TextureId % 16 + 1) / 16f;
            float v1 = (Type.TextureId / 16) / 16f;
            float v2 = (Type.TextureId / 16 + 1) / 16f;

            return new VertexP3N3T2[] {
                new VertexP3N3T2(new Vector3h(x, y, z), new Vector3h(-1, 0, 0), new Vector2h(u1, v2)),
                new VertexP3N3T2(new Vector3h(x, y + 1, z), new Vector3h(-1, 0, 0), new Vector2h(u1, v1)),
                new VertexP3N3T2(new Vector3h(x, y + 1, z + 1), new Vector3h(-1, 0, 0), new Vector2h(u2, v1)),

                new VertexP3N3T2(new Vector3h(x, y, z), new Vector3h(-1, 0, 0), new Vector2h(u1, v2)),
                new VertexP3N3T2(new Vector3h(x, y + 1, z + 1), new Vector3h(-1, 0, 0), new Vector2h(u2, v1)),
                new VertexP3N3T2(new Vector3h(x, y, z + 1), new Vector3h(-1, 0, 0), new Vector2h(u2, v2)),
            };
        }

        public VertexP3N3T2[] GenRightVertexes(int x, int y, int z)
        {
            if (!Type.GenGeometry)
            {
                return new VertexP3N3T2[0];
            }

            float u1 = (Type.TextureId % 16) / 16f;
            float u2 = (Type.TextureId % 16 + 1) / 16f;
            float v1 = (Type.TextureId / 16) / 16f;
            float v2 = (Type.TextureId / 16 + 1) / 16f;

            return new VertexP3N3T2[] {
                new VertexP3N3T2(new Vector3h(x + 1, y, z), new Vector3h(1, 0, 0), new Vector2h(u2, v2)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z + 1), new Vector3h(1, 0, 0), new Vector2h(u1, v1)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z), new Vector3h(1, 0, 0), new Vector2h(u2, v1)),

                new VertexP3N3T2(new Vector3h(x + 1, y, z), new Vector3h(1, 0, 0), new Vector2h(u2, v2)),
                new VertexP3N3T2(new Vector3h(x + 1, y, z + 1), new Vector3h(1, 0, 0), new Vector2h(u1, v2)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z + 1), new Vector3h(1, 0, 0), new Vector2h(u1, v1)),
            };
        }

        public VertexP3N3T2[] GenTopVertexes(int x, int y, int z)
        {
            if (!Type.GenGeometry)
            {
                return new VertexP3N3T2[0];
            }

            float topU1 = (Type.TopTextureId % 16) / 16f;
            float topU2 = (Type.TopTextureId % 16 + 1) / 16f;
            float topV1 = (Type.TopTextureId / 16) / 16f;
            float topV2 = (Type.TopTextureId / 16 + 1) / 16f;

            return new VertexP3N3T2[] {
                new VertexP3N3T2(new Vector3h(x, y + 1, z), new Vector3h(0, 1, 0), new Vector2h(topU1, topV1)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z), new Vector3h(0, 1, 0), new Vector2h(topU2, topV1)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z + 1), new Vector3h(0, 1, 0), new Vector2h(topU2, topV2)),

                new VertexP3N3T2(new Vector3h(x, y + 1, z), new Vector3h(0, 1, 0), new Vector2h(topU1, topV1)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z + 1), new Vector3h(0, 1, 0), new Vector2h(topU2, topV2)),
                new VertexP3N3T2(new Vector3h(x, y + 1, z + 1), new Vector3h(0, 1, 0), new Vector2h(topU1, topV2)),
            };
        }

        public VertexP3N3T2[] GenBottomVertexes(int x, int y, int z)
        {
            if (!Type.GenGeometry)
            {
                return new VertexP3N3T2[0];
            }

            float bottomU1 = (Type.BottomTextureId % 16) / 16f;
            float bottomU2 = (Type.BottomTextureId % 16 + 1) / 16f;
            float bottomV1 = (Type.BottomTextureId / 16) / 16f;
            float bottomV2 = (Type.BottomTextureId / 16 + 1) / 16f;

            return new VertexP3N3T2[] {
                new VertexP3N3T2(new Vector3h(x, y, z), new Vector3h(0, -1, 0), new Vector2h(bottomU1, bottomV1)),
                new VertexP3N3T2(new Vector3h(x + 1, y, z + 1), new Vector3h(0, -1, 0), new Vector2h(bottomU2, bottomV2)),
                new VertexP3N3T2(new Vector3h(x + 1, y, z), new Vector3h(0, -1, 0), new Vector2h(bottomU2, bottomV1)),

                new VertexP3N3T2(new Vector3h(x, y, z), new Vector3h(0, -1, 0), new Vector2h(bottomU1, bottomV1)),
                new VertexP3N3T2(new Vector3h(x, y, z + 1), new Vector3h(0, -1, 0), new Vector2h(bottomU1, bottomV2)),
                new VertexP3N3T2(new Vector3h(x + 1, y, z + 1), new Vector3h(0, -1, 0), new Vector2h(bottomU2, bottomV2)),
            };
        }

        public VertexP3N3T2[] GenFrontVertexes(int x, int y, int z)
        {
            if (!Type.GenGeometry)
            {
                return new VertexP3N3T2[0];
            }

            float u1 = (Type.TextureId % 16) / 16f;
            float u2 = (Type.TextureId % 16 + 1) / 16f;
            float v1 = (Type.TextureId / 16) / 16f;
            float v2 = (Type.TextureId / 16 + 1) / 16f;

            return new VertexP3N3T2[] {
                new VertexP3N3T2(new Vector3h(x, y, z), new Vector3h(0, 0, -1), new Vector2h(u2, v2)),
                new VertexP3N3T2(new Vector3h(x + 1, y, z), new Vector3h(0, 0, -1), new Vector2h(u1, v2)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z), new Vector3h(0, 0, -1), new Vector2h(u1, v1)),

                new VertexP3N3T2(new Vector3h(x, y, z), new Vector3h(0, 0, -1), new Vector2h(u2, v2)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z), new Vector3h(0, 0, -1), new Vector2h(u1, v1)),
                new VertexP3N3T2(new Vector3h(x, y + 1, z), new Vector3h(0, 0, -1), new Vector2h(u2, v1)),
            };
        }

        public VertexP3N3T2[] GenBackVertexes(int x, int y, int z)
        {
            if (!Type.GenGeometry)
            {
                return new VertexP3N3T2[0];
            }

            float u1 = (Type.TextureId % 16) / 16f;
            float u2 = (Type.TextureId % 16 + 1) / 16f;
            float v1 = (Type.TextureId / 16) / 16f;
            float v2 = (Type.TextureId / 16 + 1) / 16f;

            return new VertexP3N3T2[] {
                new VertexP3N3T2(new Vector3h(x, y, z + 1), new Vector3h(0, 0, 1), new Vector2h(u1, v2)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z + 1), new Vector3h(0, 0, 1), new Vector2h(u2, v1)),
                new VertexP3N3T2(new Vector3h(x + 1, y, z + 1), new Vector3h(0, 0, 1), new Vector2h(u2, v2)),

                new VertexP3N3T2(new Vector3h(x, y, z + 1), new Vector3h(0, 0, 1), new Vector2h(u1, v2)),
                new VertexP3N3T2(new Vector3h(x, y + 1, z + 1), new Vector3h(0, 0, 1), new Vector2h(u1, v1)),
                new VertexP3N3T2(new Vector3h(x + 1, y + 1, z + 1), new Vector3h(0, 0, 1), new Vector2h(u2, v1)),
            };
        }
    }
}
