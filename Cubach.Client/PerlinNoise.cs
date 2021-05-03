using OpenTK.Mathematics;
using System;

namespace Cubach.Client
{
    public static class PerlinNoise
    {
        private static float SCurve(float t)
        {
            return t * t * (3 - 2 * t);
        }

        private static Vector2 SCurve2(Vector2 t)
        {
            return new Vector2(SCurve(t.X), SCurve(t.Y));
        }

        private static Vector3 SCurve3(Vector3 t)
        {
            return new Vector3(SCurve(t.X), SCurve(t.Y), SCurve(t.Z));
        }

        private static long Random2(long x, long y)
        {
            x = x * 3266489917 + 374761393;
            x = (x << 17) | (x >> 15);

            x += y * 3266489917;

            x *= 66826563;
            x ^= x >> 15;
            x *= 2246822519;
            x ^= x >> 13;
            x *= 3266489917;
            x ^= x >> 16;

            return x;
        }

        private static Vector2 RandomUnit2(int x, int y)
        {
            int t = (int)Random2(x, y);

            return Coords.PolarToCartesian(1, t);
        }

        public static float Gen2(Vector2 position)
        {
            Vector2 p = new Vector2((int)Math.Floor(position.X), (int)Math.Floor(position.Y));
            Vector2 d = position - p;
            float[,] values = new float[2, 2];
            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < 2; ++j)
                {
                    Vector2 gradient = RandomUnit2((int)p.X + i, (int)p.Y + j);
                    Vector2 distance = new Vector2(d.X - i, d.Y - j);
                    values[i, j] = Vector2.Dot(gradient, distance);
                }
            }

            Vector2 t = SCurve2(d);

            return Interpolation.BiLerp(values[0, 0], values[1, 0], values[0, 1], values[1, 1], t);
        }
    }
}
