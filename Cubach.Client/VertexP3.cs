using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Cubach.Client
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexP3
    {
        public Vector3 Position;

        public static readonly VertexAttrib[] VertexAttribs = new[] {
            new VertexAttrib(0, 3, VertexAttribPointerType.Float, normalized: false, Marshal.SizeOf<float>() * 3, 0),
        };

        public VertexP3(Vector3 position)
        {
            Position = position;
        }
    }
}
