using System;
using OpenTK.Graphics.OpenGL4;

namespace Cubach.Client
{
    public sealed class Mesh<T> : IDisposable where T : struct
    {
        public T[] Vertexes { get; private set; }
        public VertexArray VertexArray { get; private set; }
        public VertexBuffer VertexBuffer { get; private set; }

        public Mesh(T[] vertexes)
        {
            Vertexes = vertexes;

            if (vertexes.Length > 0)
            {
                VertexArray = new VertexArray();
                VertexBuffer = new VertexBuffer();
                VertexBuffer.SetData(vertexes);
            }
        }

        public void SetVertexAttribs(VertexAttrib[] vertexAttribs)
        {
            VertexArray?.SetVertexAttribs(VertexBuffer.Handle, vertexAttribs);
        }

        public void Draw(PrimitiveType type = PrimitiveType.Triangles)
        {
            VertexArray?.Draw(type, 0, Vertexes.Length);
        }

        public void Dispose()
        {
            VertexArray?.Dispose();
            VertexBuffer?.Dispose();
        }
    }
}
