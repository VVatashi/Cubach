using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using GDIPixelFormat = System.Drawing.Imaging.PixelFormat;
using GLPixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Cubach.Client
{
    public sealed class TextureCubemap : IDisposable
    {
        public int Handle { get; private set; }

        public TextureCubemap()
        {
            Handle = GL.GenTexture();
        }

        public void Bind(TextureUnit textureUnit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(textureUnit);
            GL.BindTexture(TextureTarget.TextureCubeMap, Handle);
        }

        public static void Unbind(TextureUnit textureUnit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(textureUnit);
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        }

        public void SetImages(Bitmap[] images, TextureUnit textureUnit = TextureUnit.Texture0)
        {
            Bind(textureUnit);

            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            for (int i = 0; i < 6; ++i)
            {
                int width = images[i].Width;
                int height = images[i].Height;

                BitmapData data = images[i].LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, GDIPixelFormat.Format24bppRgb);
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb, width, height, 0, GLPixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                images[i].UnlockBits(data);
            }
        }

        public void Dispose()
        {
            GL.DeleteTexture(Handle);
            GC.SuppressFinalize(this);
        }

        ~TextureCubemap()
        {
            GL.DeleteTexture(Handle);
        }
    }
}
