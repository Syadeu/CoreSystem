using System;
//using System.Numerics;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Syadeu.ThreadSafe
{
    [System.Obsolete("Deprecated", true)]
    public static class ThreadSafe
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatIntUnion
        {
            [FieldOffset(0)]
            public double x;
            [FieldOffset(0)]
            public int i;
        }
        private static FloatIntUnion union = new FloatIntUnion();
        //Fast inverse Sqrt
        internal static double InvSqrt(double x)
        {
            union.x = x;
            union.i = 0x5f3759df - (union.i >> 1);
            x = union.x;
            x *= (1.5f - 0.5f * x * x * x);
            return x;
        }

        public static Vector3 ToThreadSafe(this UnityEngine.Vector3 vector3)
        {
            return new Vector3(vector3.x, vector3.y, vector3.z);
        }
        public static Vector2 ToThreadSafe(this UnityEngine.Vector2 vector2)
        {
            return new Vector2(vector2);
        }
        public static Vector3 ToThreadSafe(this float3 float3) => new Vector3(float3);

        public static class Random
        {
            private static System.Random m_Random = new System.Random();
            public static float Range(float a, float b)
            {
                int first = m_Random.Next((int)a, (int)b);
                double last = m_Random.NextDouble();

                double sum = first + last;
                return (float)Math.Min(sum, b);
            }
            public static int Range(int a, int b)
            {
                return m_Random.Next(a, b);
            }
        }
    }
} 
