using OpenTK.Graphics.OpenGL4;
using System;

namespace Cubach.Client
{
    public class ShaderProgram : IDisposable
    {
        public int Handle { get; private set; }

        public ShaderProgram()
        {
            Handle = GL.CreateProgram();
        }

        public void Attach(Shader shader)
        {
            GL.AttachShader(Handle, shader.Handle);
        }

        public bool Link()
        {
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int shaderProgramStatus);

            return shaderProgramStatus != 0;
        }

        public string GetError()
        {
            return GL.GetProgramInfoLog(Handle);
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
            GC.SuppressFinalize(this);
        }

        ~ShaderProgram()
        {
            GL.DeleteProgram(Handle);
        }
    }
}
