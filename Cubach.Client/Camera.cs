using OpenTK.Mathematics;

namespace Cubach.Client
{
    public class Camera
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public Vector3 GetFront()
        {
            return Rotation * -Vector3.UnitZ;
        }

        public Vector3 GetRight()
        {
            return Vector3.Cross(GetFront(), Vector3.UnitY);
        }

        public Vector3 GetUp()
        {
            return Vector3.Cross(GetRight(), GetFront());
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.CreateTranslation(-Position) * Matrix4.CreateFromQuaternion(Rotation.Inverted());
        }
    }
}
