using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Cubach.Client
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexP3N3T2
    {
        public Vector3h Position;
        public Vector3h Normal;
        public Vector2h TexCoords;

        public static readonly VertexAttrib[] VertexAttribs = new[] {
            new VertexAttrib(0, 3, VertexAttribPointerType.HalfFloat, normalized: false, Marshal.SizeOf<float>() * 8 / 2, 0),
            new VertexAttrib(1, 3, VertexAttribPointerType.HalfFloat, normalized: true, Marshal.SizeOf<float>() * 8 / 2, Marshal.SizeOf<float>() * 3 / 2),
            new VertexAttrib(2, 2, VertexAttribPointerType.HalfFloat, normalized: true, Marshal.SizeOf<float>() * 8 / 2, Marshal.SizeOf<float>() * 6 / 2),
        };

        public VertexP3N3T2(Vector3h position, Vector3h normal, Vector2h texCoords)
        {
            Position = position;
            Normal = normal;
            TexCoords = texCoords;
        }
    }
}
