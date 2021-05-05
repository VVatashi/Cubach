using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Cubach.Client
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexP3C4
    {
        public Vector3 Position;
        public Color4 Color;

        public static readonly VertexAttrib[] VertexAttribs = new[] {
            new VertexAttrib(0, 3, VertexAttribPointerType.Float, normalized: false, Marshal.SizeOf<float>() * 7, 0),
            new VertexAttrib(1, 4, VertexAttribPointerType.Float, normalized: false, Marshal.SizeOf<float>() * 7, Marshal.SizeOf<float>() * 3),
        };

        public VertexP3C4(Vector3 position, Color4 color)
        {
            Position = position;
            Color = color;
        }
    }
}
