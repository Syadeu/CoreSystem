using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.ThreadSafe
{
    public struct Vector2 : IEqualityComparer<Vector2>, IEqualityComparer
    {
        public static Vector2 Zero = new Vector2(0, 0);
        public static Vector2 One = new Vector2(1, 1);

        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public Vector2(UnityEngine.Vector2 vector2)
        {
            x = vector2.x;
            y = vector2.y;
        }

        public double SqrMagnitute
        {
            get
            {
                return Math.Pow(x, 2) + Math.Pow(y, 2);
            }
        }
        public Vector2 Normalize
        {
            get
            {
                double inversedMagnitude = ThreadSafe.InvSqrt(SqrMagnitute);
                return this * (float)inversedMagnitude;
            }
        }

        public UnityEngine.Vector2 ToUnity()
        {
            return new UnityEngine.Vector2(x, y);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public bool Equals(Vector2 x, Vector2 y)
        {
            if (x.x == y.x && x.y == y.y) return true;
            return false;
        }
        public new bool Equals(object x, object y)
        {
            Vector2 a = (Vector2)x;
            Vector2 b = (Vector2)y;

            if (a.x == b.x && a.y == b.y) return true;
            return false;
        }

        public int GetHashCode(Vector2 obj) => obj.GetHashCode();
        public int GetHashCode(object obj) => obj.GetHashCode();

        public static implicit operator UnityEngine.Vector2(Vector2 a)
        {
            return a.ToUnity();
        }
        public static implicit operator UnityEngine.Vector3(Vector2 a)
        {
            return new UnityEngine.Vector3(a.x, a.y, 0);
        }
        public static Vector2 operator *(Vector2 a, float b)
        {
            return new Vector2(a.x * b, a.y * b);
        }
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x - b.x, a.y - b.y);
        }
        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x + b.x, a.y + b.y);
        }
    }
}
