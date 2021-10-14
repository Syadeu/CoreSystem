using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Syadeu.Collections.Lua
{
    internal sealed class LuaVectorUtils
    {
        internal static Vector3 ToVector(double[] vs) => new Vector3((float)vs[0], (float)vs[1], (float)vs[2]);
        internal static double[] FromVector(Vector3 vec) => new double[] { vec.x, vec.y, vec.z };

        public static double[] ToVector2(float a, float b) => new double[] { a, b };
        public static double[] ToVector3(float a, float b, float c) => new double[] { a, b, c };

        public static double[] Lerp(double[] a, double[] b, float t)
            => FromVector(Vector3.Lerp(ToVector(a), ToVector(b), t));
    }
}
