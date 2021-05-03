using OpenTK.Graphics.OpenGL4;
using System;
using System.Runtime.InteropServices;

namespace Cubach.Client
{
    public sealed class VertexBuffer : IDisposable
    {
        public int Handle { get; private set; }
        public BufferTarget Target { get; private set; }

        public VertexBuffer(BufferTarget target = BufferTarget.ArrayBuffer)
        {
            Handle = GL.GenBuffer();
            Target = target;
        }

        public void Bind()
        {
            GL.BindBuffer(Target, Handle);
        }

        public static void Unbind(BufferTarget target = BufferTarget.ArrayBuffer)
        {
            GL.BindBuffer(target, 0);
        }

        public void SetData<T>(T[] vertexes, BufferUsageHint hint = BufferUsageHint.StaticDraw) where T : struct
        {
            if (vertexes.Length == 0)
            {
                throw new ArgumentException(nameof(vertexes), "VertexBuffer must have at least one vertex.");
            }

            Bind();
            GL.BufferData(Target, Marshal.SizeOf<T>() * vertexes.Length, vertexes, hint);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(Handle);
            GC.SuppressFinalize(this);
        }

        ~VertexBuffer()
        {
            GL.DeleteBuffer(Handle);
        }
    }
}
