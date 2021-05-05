using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Cubach.Client
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexP3N3T2
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoords;

        public static readonly VertexAttrib[] VertexAttribs = new[] {
            new VertexAttrib(0, 3, VertexAttribPointerType.Float, normalized: false, Marshal.SizeOf<float>() * 8, 0),
            new VertexAttrib(1, 3, VertexAttribPointerType.Float, normalized: true, Marshal.SizeOf<float>() * 8, Marshal.SizeOf<float>() * 3),
            new VertexAttrib(2, 2, VertexAttribPointerType.Float, normalized: true, Marshal.SizeOf<float>() * 8, Marshal.SizeOf<float>() * 6),
        };

        public VertexP3N3T2(Vector3 position, Vector3 normal, Vector2 texCoords)
        {
            Position = position;
            Normal = normal;
            TexCoords = texCoords;
        }
    }
}
