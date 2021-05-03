using OpenTK.Mathematics;

namespace Cubach.Client
{
    public static class MultiOctaveNoise
    {
        public static float Gen2(Vector2 position)
        {
            const float freq = 128;
            const int octaves = 5;

            float result = 0;

            for (int i = 0; i < octaves; ++i)
            {
                int d = 1 << i;
                result += PerlinNoise.Gen2(position * d / freq) / d;
            }

            return result;
        }
    }
}
