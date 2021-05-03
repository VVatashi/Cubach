using OpenTK.Graphics.OpenGL4;

namespace Cubach.Client
{
    public sealed class VertexAttrib
    {
        public readonly int Index;
        public readonly int Elements;
        public readonly VertexAttribPointerType Type;
        public readonly bool Normalized;
        public readonly int Stride;
        public readonly int Offset;

        public VertexAttrib(int index, int elements, VertexAttribPointerType type, bool normalized, int stride, int offset)
        {
            Index = index;
            Elements = elements;
            Type = type;
            Normalized = normalized;
            Stride = stride;
            Offset = offset;
        }
    }
}
