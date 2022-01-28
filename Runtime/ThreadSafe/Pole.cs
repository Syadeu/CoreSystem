using System;

namespace Syadeu.ThreadSafe
{
    [System.Obsolete("Deprecated", true)]
    [Serializable]
    public struct Pole
    {
        public static Pole NONE = new Pole() { Disabled = true, Name = "NULL" };

        public string Name;
        public bool Disabled;

        [UnityEngine.Space]
        public Vector3 PointA;
        public Vector3 PointB;

        public UnityEngine.Transform Parent;
        public Vector3 Center;

        public UnityEngine.Vector3 Direction;
        public Vector3 LineCenter
        {
            get
            {
                return PointA + ((PointB - PointA) * .5f);
            }
        }
        public Vector3 Forward
        {
            get
            {
                return Center - LineCenter;
            }
        }

        public Pole(UnityEngine.Transform parent, Vector3 center, Vector3 forward, Vector3 poleDir,
            //ThreadSafe.Vector3 min, ThreadSafe.Vector3 max,
            float lineSize,
            float linePos)
        {
            Name = "new pole";

            linePos *= .5f;
            lineSize *= .5f;

            Parent = parent;

            Center = center;
            var LineCenter = center - (forward * linePos) - (forward /** 2*/);
            PointA = LineCenter;
            PointA += new Vector3(poleDir) * lineSize;
            PointB = LineCenter;
            PointB -= new Vector3(poleDir) * lineSize;

            Direction = PointA - PointB;

            Disabled = false;
        }

        public bool Intersect(Vector3 origin, Vector3 dir, out Vector3 position)
        {
            position = default;

            Vector3 p0 = Parent.TransformPoint(PointA).ToThreadSafe();
            p0.y = Center.y;
            Vector3 p1 = Parent.TransformPoint(PointB).ToThreadSafe();
            p1.y = Center.y;

            origin.y = Center.y;

            Vector3 up = Parent.TransformPoint(PointA).ToThreadSafe();
            up.y += 2;

            if (Bound.IntersectTriangle(origin, dir, p0, p1, up, out var distance))
            {
                position = origin + (dir * distance);
                //Gizmos.DrawSphere(position, 0.1f);
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public Pole DrawGizmos(UnityEngine.Transform parent)
        {
            if (Disabled) return this;

            UnityEngine.Gizmos.DrawCube(parent.TransformPoint(Center), Vector3.One * 0.1f);
            UnityEngine.Gizmos.DrawCube(parent.TransformPoint(LineCenter), Vector3.One * 0.1f);

            UnityEngine.Gizmos.DrawLine(parent.TransformPoint(PointA), parent.TransformPoint(PointB));
            UnityEngine.Gizmos.DrawCube(parent.TransformPoint(PointA), Vector3.One * 0.1f);
            UnityEngine.Gizmos.DrawCube(parent.TransformPoint(PointB), Vector3.One * 0.1f);

            return this;
        }
#endif
    }
}
