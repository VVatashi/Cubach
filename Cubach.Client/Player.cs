using OpenTK.Mathematics;

namespace Cubach.Client
{
    public class Player
    {
        public static Vector3 Size = new Vector3(0.8f, 1.8f, 0.8f);

        public Vector3 Position;
        public Vector3 Velocity;

        public Player(Vector3 position)
        {
            Position = position;
            Velocity = Vector3.Zero;
        }

        public Player() : this(Vector3.Zero) { }

        public AABB AABB
        {
            get
            {
                Vector3 halfSize = 0.5f * Size;

                return new AABB(Position - halfSize, Position + halfSize);
            }
        }
    }
}
