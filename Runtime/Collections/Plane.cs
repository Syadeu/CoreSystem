// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Globalization;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Collections
{
    public struct Plane
    {
        // sizeof(Plane) is not const in C# and so cannot be used in fixed arrays, so we define it here
        internal const int size = 16;

        float3 m_Normal;
        float m_Distance;

        /// <summary>
        /// Normal vector of the plane.
        /// </summary>
        public float3 normal
        {
            get { return m_Normal; }
            set { m_Normal = value; }
        }
        /// <summary>
        /// Distance from the origin to the plane.
        /// </summary>
        public float distance
        {
            get { return m_Distance; }
            set { m_Distance = value; }
        }

        // Creates a plane.
        public Plane(float3 inNormal, float3 inPoint)
        {
            m_Normal = math.normalize(inNormal);
            m_Distance = -math.dot(m_Normal, inPoint);
        }

        // Creates a plane.
        public Plane(float3 inNormal, float d)
        {
            m_Normal = math.normalize(inNormal);
            m_Distance = d;
        }

        // Creates a plane.
        public Plane(float3 a, float3 b, float3 c)
        {
            m_Normal = math.normalize(math.cross(b - a, c - a));
            m_Distance = -math.dot(m_Normal, a);
        }

        // Sets a plane using a point that lies within it plus a normal to orient it (note that the normal must be a normalized vector).
        public void SetNormalAndPosition(float3 inNormal, float3 inPoint)
        {
            m_Normal = math.normalize(inNormal);
            m_Distance = -math.dot(inNormal, inPoint);
        }

        // Sets a plane using three points that lie within it.  The points go around clockwise as you look down on the top surface of the plane.
        public void Set3Points(float3 a, float3 b, float3 c)
        {
            m_Normal = math.normalize(math.cross(b - a, c - a));
            m_Distance = -math.dot(m_Normal, a);
        }

        /// <summary>
        /// Make the plane face the opposite direction
        /// </summary>
        public void Flip() { m_Normal = -m_Normal; m_Distance = -m_Distance; }

        /// <summary>
        /// Return a version of the plane that faces the opposite direction
        /// </summary>
        public Plane flipped { get { return new Plane(-m_Normal, -m_Distance); } }

        /// <summary>
        /// Translates the plane into a given direction
        /// </summary>
        /// <param name="translation"></param>
        public void Translate(float3 translation) { m_Distance += math.dot(m_Normal, translation); }

        /// <summary>
        /// Creates a plane that's translated into a given direction
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="translation"></param>
        /// <returns></returns>
        public static Plane Translate(Plane plane, float3 translation) { return new Plane(plane.m_Normal, plane.m_Distance += math.dot(plane.m_Normal, translation)); }

        /// <summary>
        /// Calculates the closest point on the plane.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float3 ClosestPointOnPlane(float3 point)
        {
            var pointToPlaneDistance = math.dot(m_Normal, point) + m_Distance;
            return point - (m_Normal * pointToPlaneDistance);
        }

        /// <summary>
        /// Returns a signed distance from plane to point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float GetDistanceToPoint(float3 point) { return math.dot(m_Normal, point) + m_Distance; }

        /// <summary>
        /// Is a point on the positive side of the plane?
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool GetSide(float3 point) { return math.dot(m_Normal, point) + m_Distance > 0.0F; }

        /// <summary>
        /// Are two points on the same side of the plane?
        /// </summary>
        /// <param name="inPt0"></param>
        /// <param name="inPt1"></param>
        /// <returns></returns>
        public bool SameSide(float3 inPt0, float3 inPt1)
        {
            float d0 = GetDistanceToPoint(inPt0);
            float d1 = GetDistanceToPoint(inPt1);
            return (d0 > 0.0f && d1 > 0.0f) ||
                (d0 <= 0.0f && d1 <= 0.0f);
        }

        /// <summary>
        /// Intersects a ray with the plane.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="enter"></param>
        /// <returns></returns>
        public bool Raycast(Ray ray, out float enter)
        {
            float vdot = math.dot(ray.direction, m_Normal);
            float ndot = -math.dot(ray.origin, m_Normal) - m_Distance;

            
            if (Approximately(vdot, 0.0f))
            {
                enter = 0.0F;
                return false;
            }

            enter = ndot / vdot;

            return enter > 0.0F;
        }
        /// <summary>
        /// Projects a vector onto a plane defined by a normal orthogonal to the plane.
        /// </summary>
        /// <param name="point">The location of the vector above the plane.</param>
        /// <returns></returns>
        public float3 Project(float3 point)
        {
            float num = math.dot(m_Normal, m_Normal);
            if (num < math.EPSILON)
            {
                return point;
            }

            float num2 = math.dot(point, m_Normal);
            return new float3(
                point.x - m_Normal.x * num2 / num,
                point.y - m_Normal.y * num2 / num,
                point.z - m_Normal.z * num2 / num);
        }

        public override string ToString() => ToString(null, CultureInfo.InvariantCulture.NumberFormat);
        public string ToString(string format) => ToString(format, CultureInfo.InvariantCulture.NumberFormat);
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F1";
            return string.Format("(normal:{0}, distance:{1})", m_Normal.ToString(format, formatProvider), m_Distance.ToString(format, formatProvider));
        }

        // Compares two floating point values if they are similar.
        private static bool Approximately(float a, float b)
        {
            // If a or b is zero, compare that the other is less or equal to epsilon.
            // If neither a or b are 0, then find an epsilon that is good for
            // comparing numbers at the maximum magnitude of a and b.
            // Floating points have about 7 significant digits, so
            // 1.000001f can be represented while 1.0000001f is rounded to zero,
            // thus we could use an epsilon of 0.000001f for comparing values close to 1.
            // We multiply this epsilon by the biggest magnitude of a and b.
            return math.abs(b - a) < math.max(0.000001f * math.max(math.abs(a), math.abs(b)), math.EPSILON * 8);
        }
    }
}
