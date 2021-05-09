using OpenTK.Mathematics;
using System;

namespace Cubach.Client
{
    public struct AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public AABB(Vector3 min, Vector3 max)
        {
            Min = new Vector3(MathF.Min(min.X, max.X), MathF.Min(min.Y, max.Y), MathF.Min(min.Z, max.Z));
            Max = new Vector3(MathF.Max(min.X, max.X), MathF.Max(min.Y, max.Y), MathF.Max(min.Z, max.Z));
        }
    }
}
