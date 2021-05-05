using OpenTK.Graphics.OpenGL4;
using System;

namespace Cubach.Client
{
    public class Shader : IDisposable
    {
        public int Handle { get; private set; }

        public Shader(ShaderType type)
        {
            Handle = GL.CreateShader(type);
        }

        public bool Compile(string source)
        {
            GL.ShaderSource(Handle, source);
            GL.CompileShader(Handle);

            GL.GetShader(Handle, ShaderParameter.CompileStatus, out int shaderStatus);

            return shaderStatus != 0;
        }

        public string GetError()
        {
            return GL.GetShaderInfoLog(Handle);
        }

        public void Dispose()
        {
            GL.DeleteShader(Handle);
            GC.SuppressFinalize(this);
        }

        ~Shader()
        {
            GL.DeleteShader(Handle);
        }
    }
}
