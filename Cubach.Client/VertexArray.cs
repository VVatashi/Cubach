using OpenTK.Graphics.OpenGL4;
using System;

namespace Cubach.Client
{
    public sealed class VertexArray : IDisposable
    {
        public int Handle { get; private set; }

        public VertexArray()
        {
            Handle = GL.GenVertexArray();
        }

        public void Bind()
        {
            GL.BindVertexArray(Handle);
        }

        public static void Unbind()
        {
            GL.BindVertexArray(0);
        }

        public void SetVertexAttribs(int vertexBufferHandle, VertexAttrib[] vertexAttribs)
        {
            Bind();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);

            foreach (VertexAttrib vertexAttrib in vertexAttribs)
            {
                GL.EnableVertexAttribArray(vertexAttrib.Index);
                GL.VertexAttribPointer(vertexAttrib.Index, vertexAttrib.Elements, vertexAttrib.Type, vertexAttrib.Normalized, vertexAttrib.Stride, vertexAttrib.Offset);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Unbind();
        }

        public void Draw(PrimitiveType type, int first, int count)
        {
            Bind();
            GL.DrawArrays(type, first, count);
            Unbind();
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(Handle);
            GC.SuppressFinalize(this);
        }

        ~VertexArray()
        {
            GL.DeleteVertexArray(Handle);
        }
    }
}
