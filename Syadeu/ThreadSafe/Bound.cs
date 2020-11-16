using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.ThreadSafe
{
    /// <summary>
    /// 스레드 세이프용 로테이팅 바운드입니다
    /// </summary>
    public struct Bound
    {
        // R(x) = origin + (dir * t)
        // N o (P0 - P1) = 0

        /// <summary>
        /// World position only
        /// </summary>
        public Vector3 Center;
        /// <summary>
        /// World position only
        /// </summary>
        public Vector3[] Positions;
        public float Size;

        public float XSizeOffset;
        public float ZSizeOffset;

        public float YPositionOffset;

        public Bound(Vector3 center, float size, params Vector3[] positions)
        {
            if (positions.Length < 2)
            {
                "more then 2 only".ToLogError();
            }

            Center = center;
            Positions = positions;
            Size = size;

            XSizeOffset = 1;
            ZSizeOffset = 1;

            YPositionOffset = 0;
        }
        public Bound(Vector3 center, float size,
            float xSizeOffset,
            float zSizeOffset,
            float yPositionOffset,
            params Vector3[] positions)
        {
            if (positions.Length < 2)
            {
                "more then 2 only".ToLogError();
            }

            Center = center;
            Positions = positions;
            Size = size;

            XSizeOffset = xSizeOffset;
            ZSizeOffset = zSizeOffset;

            YPositionOffset = yPositionOffset;
        }

        private Vector3[] GetVertices()
        {
            List<Vector3> list = new List<Vector3>();
            for (int i = 0; i < Positions.Length; i++)
            {
                if (i + 2 > Positions.Length) break;

                Vector3 side1 = Center - Positions[i];
                Vector3 side2 = Positions[i + 1] - Positions[i];

                Vector3 up = side1 * side2;
                Vector3 centerCross = Positions[i] - ((Center + Positions[i + 1]) * 0.5f);

                Vector3 yoffset = new Vector3(0, YPositionOffset, 0);

                list.Add((Positions[i]) + (up * Size * ZSizeOffset) + (centerCross * Size * XSizeOffset) + yoffset);
                list.Add((Positions[i]) + (-up * Size * ZSizeOffset) + (centerCross * Size * XSizeOffset) + yoffset);
                list.Add((Positions[i]) + (-up * Size * ZSizeOffset) + (-centerCross * Size * XSizeOffset) + yoffset);
                list.Add((Positions[i]) + (up * Size * ZSizeOffset) + (-centerCross * Size * XSizeOffset) + yoffset);

                list.Add((Positions[i + 1]) + (up * Size * ZSizeOffset) + (centerCross * Size * XSizeOffset) + yoffset);
                list.Add((Positions[i + 1]) + (-up * Size * ZSizeOffset) + (centerCross * Size * XSizeOffset) + yoffset);
                list.Add((Positions[i + 1]) + (-up * Size * ZSizeOffset) + (-centerCross * Size * XSizeOffset) + yoffset);
                list.Add((Positions[i + 1]) + (up * Size * ZSizeOffset) + (-centerCross * Size * XSizeOffset) + yoffset);
            }

            return list.ToArray();
        }

        public Vector3 ClosestPointOnPlane(Vector3 origin, out float distance)
        {
            Vector3[] vertices = GetVertices();
            Vector3 square1 = vertices[0];
            Vector3 square2 = vertices[0];
            Vector3 square3 = vertices[0];

            foreach (var item in vertices)
            {
                float target = (origin - item).SqrMagnitute;
                if (target < (origin - square1).SqrMagnitute)
                {
                    square3 = square2;
                    square2 = square1;

                    square1 = item;
                }
                else if (target < (origin - square2).SqrMagnitute)
                {
                    square3 = square2;

                    square2 = item;
                }
                else if (target < (origin - square3).SqrMagnitute)
                {
                    square3 = item;
                }
            }

            Vector3 side1 = square2 - square1;
            Vector3 side2 = square3 - square1;
            Vector3 up = side1 * side2;
            Vector3 pointDir = square1 - origin;

            float dot = Vector3.Dot(up, pointDir);
            distance = (float)(Math.Abs(dot) / Math.Sqrt(up.SqrMagnitute));

            float tdistance = (Vector3.Dot(up, square2) - Vector3.Dot(up, origin)) / Vector3.Dot(up, -(up * dot));
            Vector3 result = origin + (-(up * dot) * tdistance);

            return result;
        }
        /// <summary>
        /// 임의의 공간에서 입력된 꼭지점으로 계산한 평면과의 거리를 구합니다<br/>
        /// 계산 비용이 크므로 백그라운드에서 돌리는걸 추천
        /// </summary>
        /// <param name="RayOrigin"></param>
        /// <param name="RayDirection"></param>
        /// <param name="V0"></param>
        /// <param name="V1"></param>
        /// <param name="V2"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IntersectTriangle(Vector3 RayOrigin, Vector3 RayDirection, Vector3 V0, Vector3 V1, Vector3 V2, out float distance, bool isLimited = true)
        {
            distance = 0;
            Vector3 edge1 = V1 - V0;
            Vector3 edge2 = V2 - V0;

            Vector3 pvec = RayDirection * edge2;

            float dot = Vector3.Dot(edge1, pvec);

            Vector3 tvec;
            if (dot > 0) tvec = RayOrigin - V0;
            else
            {
                tvec = V0 - RayOrigin;
                dot = -dot;
            }

            if (dot < 0.0001f)
                return false;

            float u = Vector3.Dot(tvec, pvec);
            if (isLimited)
            {
                if (u < 0.0f || u > dot)
                    return false;
            }

            Vector3 qvec = tvec * edge1;

            float v = Vector3.Dot(RayDirection, qvec);
            if (isLimited)
            {
                if (v < 0.0f || u + v > dot)
                    return false;
            }

            float t = Vector3.Dot(edge2, qvec);
            float flnvDet = 1.0f / dot;

            t *= flnvDet;
            //u *= flnvDet;
            //v *= flnvDet;

            distance = t;
            return true;
        }

#if UNITY_EDITOR
        public void DrawGizmos()
        {
            //Vector3 side1 = Center - Positions[0];
            //Vector3 side2 = Positions[Positions.Length - 1] - Positions[0];

            //Vector3 up = side1 * side2;
            //Vector3 centerCross = Positions[0] - ((Center + Positions[Positions.Length - 1]) * 0.5f);

            //Gizmos.color = new Color(1, 0, 0, 0.5f);
            //Gizmos.DrawCube(Center, Vector3.One * 0.05f);
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawCube(Positions[0], Vector3.One * 0.05f);
            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawCube(Positions[Positions.Length - 1], Vector3.One * 0.05f);

            //Gizmos.color = new Color(0, 1, 0, 0.5f);
            //Gizmos.DrawLine(Positions[0] + (up * 5), Positions[0] + (-up * 5));
            //Gizmos.DrawLine(Positions[0] + (centerCross * 5), Positions[0] + (-centerCross * 5));

            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Vector3[] vertices = GetVertices();

            for (int i = 0; i < vertices.Length; i++)
            {
                Gizmos.DrawLine(vertices[i], vertices[i + 1]);
                if (i + 4 < vertices.Length)
                {
                    Gizmos.DrawLine(vertices[i], vertices[i + 4]);
                }

                Gizmos.DrawLine(vertices[i + 1], vertices[i + 2]);
                if (i + 5 < vertices.Length)
                {
                    Gizmos.DrawLine(vertices[i + 1], vertices[i + 5]);
                }

                Gizmos.DrawLine(vertices[i + 2], vertices[i + 3]);
                if (i + 6 < vertices.Length)
                {
                    Gizmos.DrawLine(vertices[i + 2], vertices[i + 6]);
                }

                Gizmos.DrawLine(vertices[i + 3], vertices[i]);
                if (i + 7 < vertices.Length)
                {
                    Gizmos.DrawLine(vertices[i + 3], vertices[i + 7]);
                }

                i += 3;
            }
        }
#endif
    }
}
