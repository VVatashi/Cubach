using OpenTK.Mathematics;
using System;

namespace Cubach.Client
{
    public static class Coords
    {
        public static Vector2 PolarToCartesian(float radius, float angle)
        {
            float x = radius * MathF.Sin(angle);
            float y = radius * MathF.Cos(angle);

            return new Vector2(x, y);
        }

        public static Vector3 SphericalToCartesian(float radius, float angleV, float angleH)
        {
            float x = radius * MathF.Sin(angleV) * MathF.Cos(angleH);
            float y = radius * MathF.Sin(angleV) * MathF.Sin(angleH);
            float z = radius * MathF.Cos(angleV);

            return new Vector3(x, y, z);
        }
    }
}
