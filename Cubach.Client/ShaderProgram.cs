using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace Cubach.Client
{
    public class ShaderProgram : IDisposable
    {
        public int Handle { get; private set; }

        private Dictionary<string, int> UniformLocations = new Dictionary<string, int>();

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

        public int GetUniformLocation(string name)
        {
            if (UniformLocations.TryGetValue(name, out int value))
            {
                return value;
            }

            return UniformLocations[name] = GL.GetUniformLocation(Handle, name);
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
