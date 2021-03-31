using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.ThreadSafe
{
    [Serializable]
    public struct Vector3 : IEqualityComparer<Vector3>, IEqualityComparer, IEquatable<Vector3>
    {
        public static Vector3 Zero = new Vector3(0, 0, 0);
        public static Vector3 One = new Vector3(1, 1, 1);

        public const double Deg2Rad = 0.0174532924;
        public const double Rad2Deg = 57.29578;

        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3(UnityEngine.Vector3 vector3)
        {
            x = vector3.x;
            y = vector3.y;
            z = vector3.z;
        }

        public float SqrMagnitute
        {
            get
            {
                return (x * x) + (y * y) + (z * z);
            }
        }
        public float Magnitute { get { return (float)Math.Sqrt(SqrMagnitute); } }
        public Vector3 Normalize
        {
            get
            {
                double inversedMagnitude = ThreadSafe.InvSqrt(SqrMagnitute);
                return this * (float)inversedMagnitude;
            }
        }

        public UnityEngine.Vector3 ToUnity() => new UnityEngine.Vector3(x, y, z);
        public global::FMOD.VECTOR ToFMOD() => new global::FMOD.VECTOR { x = x, y = y, z = z };

        public static double Distance(Vector3 from, Vector3 target) => Math.Sqrt((from - target).SqrMagnitute);

        public override string ToString() => $"({x}, {y}, {z})";

        public bool Equals(Vector3 x, Vector3 y) => x.x == y.x && x.y == y.y && x.z == y.z;
        public new bool Equals(object x, object y)
        {
            Vector3 a = (Vector3)x;
            Vector3 b = (Vector3)y;

            if (a.x == b.x && a.y == b.y && a.z == b.z) return true;
            return false;
        }

        public int GetHashCode(Vector3 obj) => obj.GetHashCode();
        public int GetHashCode(object obj) => obj.GetHashCode();

        public static implicit operator UnityEngine.Vector3(Vector3 a) => a.ToUnity();
        public static implicit operator global::FMOD.VECTOR(Vector3 a) => a.ToFMOD();
        public static Vector3 operator *(Vector3 a, float b)
        {
            return new Vector3(a.x * b, a.y * b, a.z * b);
        }
        public static Vector3 operator *(Vector3 a, double b)
        {
            float c = (float)b;
            return new Vector3(a.x * c, a.y * c, a.z * c);
        }
        /// <summary>
        /// 크로스 곱, 두 거리백터의 법선을 구함
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3 operator *(Vector3 a, Vector3 b)
        {
            return new Vector3(
                (a.y * b.z) - (a.z * b.y), 
                (a.z * b.x) - (a.x * b.z), 
                (a.x * b.y) - (a.y * b.x)
                );
        }
        /// <summary>
        /// 내적, 스칼라곱
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Dot(Vector3 a, Vector3 b)
        {
            return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
        }
        public float Angle(Vector3 vec1, Vector3 vec2)
        {
            float theta = Dot(vec1, vec2) / (vec1.Magnitute * vec2.Magnitute);
            Vector3 dirAngle = vec1 * vec2;
            double angle = Math.Acos(theta) * Rad2Deg;
            if (dirAngle.z < 0.0f) angle = 360 - angle;
            //$"사잇각 : {angle}".ToLog();
            return (float)angle;
        }
        public override bool Equals(object obj)
        {
            return obj is Vector3 vector && Equals(vector);
        }
        public bool Equals(Vector3 other)
        {
            return x == other.x &&
                   y == other.y &&
                   z == other.z;
        }
        public override int GetHashCode()
        {
            int hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        public static Vector3 operator /(Vector3 a, float b)
        {
            return new Vector3(a.x / b, a.y / b, a.z / b);
        }
        public static Vector3 operator -(Vector3 a)
        {
            return a * -1;
        }
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static bool operator ==(Vector3 a, Vector3 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }
        public static bool operator !=(Vector3 a, Vector3 b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z;
        }
    }
}
