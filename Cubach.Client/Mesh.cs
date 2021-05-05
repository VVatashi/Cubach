using System;
using OpenTK.Graphics.OpenGL4;

namespace Cubach.Client
{
    public sealed class Mesh<T> : IDisposable where T : struct
    {
        public int VertexCount { get; private set; }
        public VertexArray VertexArray { get; private set; }
        public VertexBuffer VertexBuffer { get; private set; }

        public Mesh(T[] vertexes, BufferUsageHint hint = BufferUsageHint.StaticDraw)
        {
            VertexArray = new VertexArray();
            VertexBuffer = new VertexBuffer();

            SetData(vertexes, hint);
        }

        public void SetData(T[] vertexes, BufferUsageHint hint = BufferUsageHint.StaticDraw)
        {
            VertexCount = vertexes.Length;
            if (VertexCount == 0)
            {
                return;
            }

            VertexBuffer.SetData(vertexes, hint);
        }

        public void SetVertexAttribs(VertexAttrib[] vertexAttribs)
        {
            VertexArray.SetVertexAttribs(VertexBuffer.Handle, vertexAttribs);
        }

        public void Draw(PrimitiveType type = PrimitiveType.Triangles)
        {
            if (VertexCount == 0)
            {
                return;
            }

            VertexArray.Draw(type, 0, VertexCount);
        }

        public void Dispose()
        {
            VertexArray.Dispose();
            VertexBuffer.Dispose();
        }
    }
}
