using OpenTK.Mathematics;
using System;

namespace Cubach.Client
{
    public static class CollisionDetection
    {
        public static bool RayAABBIntersection3(AABB aabb, Ray ray, out Vector3 nearIntersection, out Vector3 farIntersection)
        {
            float tx1 = (aabb.Min.X - ray.Origin.X) / ray.Direction.X;
            float tx2 = (aabb.Max.X - ray.Origin.X) / ray.Direction.X;

            float tMin = MathF.Min(tx1, tx2);
            float tMax = MathF.Max(tx1, tx2);

            float ty1 = (aabb.Min.Y - ray.Origin.Y) / ray.Direction.Y;
            float ty2 = (aabb.Max.Y - ray.Origin.Y) / ray.Direction.Y;

            tMin = MathF.Max(tMin, MathF.Min(ty1, ty2));
            tMax = MathF.Min(tMax, MathF.Max(ty1, ty2));

            float tz1 = (aabb.Min.Z - ray.Origin.Z) / ray.Direction.Z;
            float tz2 = (aabb.Max.Z - ray.Origin.Z) / ray.Direction.Z;

            tMin = MathF.Max(tMin, MathF.Min(tz1, tz2));
            tMax = MathF.Min(tMax, MathF.Max(tz1, tz2));

            if (tMax > tMin)
            {
                nearIntersection = ray.Origin + ray.Direction * tMin;
                farIntersection = ray.Origin + ray.Direction * tMax;

                return true;
            }

            nearIntersection = Vector3.Zero;
            farIntersection = Vector3.Zero;

            return false;
        }
    }
}
