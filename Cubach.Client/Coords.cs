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

        public static float TaxicabDistance2(Vector2 a, Vector2 b)
        {
            return Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);
        }

        public static float TaxicabDistance3(Vector3 a, Vector3 b)
        {
            return Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y) + Math.Abs(b.Z - a.Z);
        }

        public static Vector3 GetPerpendicular(Vector3 v)
        {
            if (v.Z != 0 && -v.X != v.Y)
            {
                return new Vector3(v.Z, v.Z, -v.X - v.Y);
            }
            else
            {
                return new Vector3(-v.Y - v.Z, v.X, v.X);
            }
        }
    }
}
