using OpenTK.Mathematics;

namespace Cubach.Client
{
    public sealed class Camera
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public Camera(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Camera(Vector3 position) : this(position, Quaternion.Identity) { }

        public Vector3 Front
        {
            get => Rotation * -Vector3.UnitZ;
        }

        public Vector3 Right
        {
            get => Vector3.Cross(Front, Vector3.UnitY);
        }

        public Vector3 Up
        {
            get => Vector3.Cross(Right, Front);
        }

        public Matrix4 ViewMatrix
        {
            get => Matrix4.CreateTranslation(-Position) * Matrix4.CreateFromQuaternion(Rotation.Inverted());
        }
    }
}
