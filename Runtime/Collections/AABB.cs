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

using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Collections
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    [JsonConverter(typeof(AABBJsonConverter))]
    public struct AABB : IEquatable<AABB>
    {
        public static AABB Zero => new AABB(float3.zero, float3.zero);

        internal float3 m_Center;
        internal float3 m_Extents;

        public AABB(float3 center, float3 size)
        {
            m_Center = center;
            m_Extents = size * .5f;
        }
        public AABB(int3 center, int3 size)
        {
            m_Center = new float3(center);
            m_Extents = new float3(size) * .5f;
        }

#pragma warning disable IDE1006 // Naming Styles
        [JsonIgnore] public float3 center { get => m_Center; set { m_Center = value; } }
        [JsonIgnore] public float3 lowerCenter
        {
            get => new float3(m_Center.x, m_Center.y - m_Extents.y, m_Center.z);
        }
        [JsonIgnore] public float3 lowerCenterLeft
        {
            get => new float3(m_Center.x - m_Extents.x, m_Center.y - m_Extents.y, m_Center.z);
        }
        [JsonIgnore] public float3 lowerCenterRight
        {
            get => new float3(m_Center.x + m_Extents.x, m_Center.y - m_Extents.y, m_Center.z);
        }
        [JsonIgnore] public float3 lowerCenterUp
        {
            get => new float3(m_Center.x, m_Center.y - m_Extents.y, m_Center.z + m_Extents.z);
        }
        [JsonIgnore] public float3 lowerCenterDown
        {
            get => new float3(m_Center.x, m_Center.y - m_Extents.y, m_Center.z - m_Extents.z);
        }
        [JsonIgnore] public float3 upperCenter
        {
            get => new float3(m_Center.x, m_Center.y + m_Extents.y, m_Center.z);
        }
        [JsonIgnore] public float3 size { get => m_Extents * 2; set { m_Extents = value * 0.5F; } }
        [JsonIgnore] public float3 extents { get => m_Extents; set { m_Extents = value; } }
        [JsonIgnore] public float3 min { get => center - extents; set { SetMinMax(value, max); } }
        [JsonIgnore] public float3 max { get => center + extents; set { SetMinMax(min, value); } }

        [JsonIgnore] public Vertices vertices => GetVertices(in this);
        [JsonIgnore] public Planes planes => GetPlanes(in this);
#pragma warning restore IDE1006 // Naming Styles

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMinMax(float3 min, float3 max)
        {
            extents = (max - min) * .5f;
            center = min + extents;
        }

        public bool Contains(float3 position)
        {
            float3
                min = this.min,
                max = this.max;

            return position.x >= min.x
                && position.y >= min.y
                && position.z >= min.z
                && position.x < max.x
                && position.y < max.y
                && position.z < max.z;
        }

        public bool Intersect(Ray ray)
        {
            float3x4[] squares = GetSquares(in this);

            for (int i = 0; i < squares.Length; i++)
            {
                if (IntersectQuad(squares[i].c0, squares[i].c1, squares[i].c2, squares[i].c3, ray, out _))
                {
                    return true;
                }
            }
            return false;
        }
        public bool Intersect(Ray ray, out float distance)
        {
            distance = float.MaxValue;
            float3x4[] squares = GetSquares(in this);

            bool intersect = false;
            for (int i = 0; i < squares.Length; i++)
            {
                if (IntersectQuad(squares[i].c0, squares[i].c1, squares[i].c2, squares[i].c3, ray, out float tempDistance))
                {
                    if (tempDistance < distance)
                    {
                        distance = tempDistance;
                    }
                    intersect = true;
                }
            }
            return intersect;
        }
        public bool Intersect(Ray ray, out float distance, out float3 point)
        {
            point = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            bool intersect = Intersect(ray, out distance);
            if (intersect)
            {
                point = ray.origin + (ray.direction * distance);
            }

            return intersect;
        }
        public bool Intersect(AABB aabb)
        {
            return (min.x <= aabb.max.x) && (max.x >= aabb.min.x) &&
                (min.y <= aabb.max.y) && (max.y >= aabb.min.y) &&
                (min.z <= aabb.max.z) && (max.z >= aabb.min.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(float3 point) => SetMinMax(math.min(min, point), math.max(max, point));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(AABB aabb)
        {
            Encapsulate(aabb.center - aabb.extents);
            Encapsulate(aabb.center + aabb.extents);
        }

        public AABB Rotation(in quaternion rot, in float3 scale) => CalculateRotationWithVertices(in this, in rot, in scale);

        private static AABB CalculateRotation(in AABB aabb, in quaternion quaternion)
        {
            float3 
                originCenter = aabb.center,
                originExtents = aabb.extents,
                originMin = (-originExtents + originCenter),
                originMax = (originExtents + originCenter);
            float4x4 trMatrix = float4x4.TRS(originCenter, quaternion, originExtents);

            float3
                minPos = math.mul(trMatrix, new float4(-originExtents * 2, 1)).xyz,
                maxPos = math.mul(trMatrix, new float4(originExtents * 2, 1)).xyz;

            AABB temp = new AABB(originCenter, float3.zero);

            //temp.SetMinMax(
            //    originMin - math.abs(originMin - minPos),
            //    originMax + math.abs(originMax - maxPos));

            // TODO : 최소 width, height 값이 설정되지않아 무한대로 축소함. 수정할 것.
            temp.SetMinMax(
                originMin + (minPos - originMin),
                originMax + (maxPos - originMax));

            //temp.SetMinMax(
            //    math.min(originMin + (minPos - originMin), limitMinf),
            //    math.max(originMax + (maxPos - originMax), limitMaxf));

            return temp;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AABB CalculateRotationWithVertices(in AABB aabb, in quaternion quaternion, in float3 scale)
        {
            float3
                originCenter = aabb.center;
                //originExtents = aabb.extents;
            //float4x4 trMatrix = float4x4.TRS(originCenter, quaternion, originExtents);
            float4x4 trMatrix = float4x4.TRS(originCenter, quaternion, scale * .5f);

            AABB temp = new AABB(originCenter, float3.zero);

            float3
                min = aabb.min,
                max = aabb.max,

                a1 = new float3(min.x, max.y, min.z),
                a2 = new float3(max.x, max.y, min.z),
                a3 = new float3(max.x, min.y, min.z),

                b1 = new float3(max.x, min.y, max.z),
                b3 = new float3(min.x, max.y, max.z),
                b4 = new float3(min.x, min.y, max.z);

            temp.Encapsulate(math.mul(trMatrix, new float4((min - originCenter) * 2, 1)).xyz);
            temp.Encapsulate(math.mul(trMatrix, new float4((a1 - originCenter) * 2, 1)).xyz);
            temp.Encapsulate(math.mul(trMatrix, new float4((a2 - originCenter) * 2, 1)).xyz);
            temp.Encapsulate(math.mul(trMatrix, new float4((a3 - originCenter) * 2, 1)).xyz);
            temp.Encapsulate(math.mul(trMatrix, new float4((b1 - originCenter) * 2, 1)).xyz);
            temp.Encapsulate(math.mul(trMatrix, new float4((max - originCenter) * 2, 1)).xyz);
            temp.Encapsulate(math.mul(trMatrix, new float4((b3 - originCenter) * 2, 1)).xyz);
            temp.Encapsulate(math.mul(trMatrix, new float4((b4 - originCenter) * 2, 1)).xyz);

            return temp;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<float3> GetVertices(Allocator allocator)
        {
            NativeArray<float3> temp = new NativeArray<float3>(8, allocator);
            temp[0] = min;
            temp[1] = new float3(min.x, max.y, min.z);
            temp[2] = new float3(max.x, max.y, min.z);
            temp[3] = new float3(max.x, min.y, min.z);

            temp[4] = new float3(max.x, min.y, max.z);
            temp[5] = max;
            temp[6] = new float3(min.x, max.y, max.z);
            temp[7] = new float3(min.x, min.y, max.z);
            return temp;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetVertices(NativeArray<float3> temp)
        {
            temp[0] = min;
            temp[1] = new float3(min.x, max.y, min.z);
            temp[2] = new float3(max.x, max.y, min.z);
            temp[3] = new float3(max.x, min.y, min.z);

            temp[4] = new float3(max.x, min.y, max.z);
            temp[5] = max;
            temp[6] = new float3(min.x, max.y, max.z);
            temp[7] = new float3(min.x, min.y, max.z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vertices GetVertices(in AABB aabb)
        {
            float3 min = aabb.min;
            float3 max = aabb.max;

            return new Vertices
            {
                a0 = min,
                a1 = new float3(min.x, max.y, min.z),
                a2 = new float3(max.x, max.y, min.z),
                a3 = new float3(max.x, min.y, min.z),

                b0 = new float3(max.x, min.y, max.z),
                b1 = max,
                b2 = new float3(min.x, max.y, max.z),
                b3 = new float3(min.x, min.y, max.z),
            };
        }
        /// <summary>
        /// <see cref="Direction"/>
        /// </summary>
        /// <param name="aabb"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Planes GetPlanes(in AABB aabb)
        {
            Vertices vertices = aabb.vertices;

            return new Planes
            {
                // Up
                a0 = new Plane(vertices.a1, vertices.b2, vertices.a2),
                // Down
                a1 = new Plane(vertices.b3, vertices.a0, vertices.b0),
                // Left
                a2 = new Plane(vertices.b3, vertices.b2, vertices.a0),
                // Right
                a3 = new Plane(vertices.a3, vertices.a2, vertices.b0),
                // Forward
                a4 = new Plane(vertices.b0, vertices.b1, vertices.b3),
                // Backward
                a5 = new Plane(vertices.a0, vertices.a1, vertices.a3),
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3x4[] GetSquares(in AABB aabb)
        {
            float3
                minPos = aabb.min,
                maxPos = aabb.max;

            return new float3x4[] {
                new float3x4(
                    minPos,
                    new float3(minPos.x, minPos.y, maxPos.z),
                    new float3(maxPos.x, minPos.y, maxPos.z),
                    new float3(maxPos.x, minPos.y, minPos.z)
                    ),
                new float3x4(
                    minPos,
                    new float3(minPos.x, maxPos.y, minPos.z),
                    new float3(maxPos.x, maxPos.y, minPos.z),
                    new float3(maxPos.x, minPos.y, maxPos.z)
                    ),
                new float3x4(
                    new float3(maxPos.x, minPos.y, minPos.z),
                    new float3(maxPos.x, maxPos.y, minPos.z),
                    new float3(maxPos.x, maxPos.y, maxPos.z),
                    new float3(maxPos.x, minPos.y, maxPos.z)
                    ),
                new float3x4(
                    new float3(maxPos.x, minPos.y, maxPos.z),
                    new float3(maxPos.x, maxPos.y, maxPos.z),
                    new float3(minPos.x, maxPos.y, maxPos.z),
                    new float3(minPos.x, minPos.y, maxPos.z)
                    ),
                new float3x4(
                    new float3(minPos.x, minPos.y, maxPos.z),
                    new float3(minPos.x, maxPos.y, maxPos.z),
                    new float3(minPos.x, maxPos.y, minPos.z),
                    minPos
                    ),
                new float3x4(
                    new float3(minPos.x, maxPos.y, minPos.z),
                    new float3(minPos.x, maxPos.y, maxPos.z),
                    new float3(maxPos.x, maxPos.y, maxPos.z),
                    new float3(maxPos.x, maxPos.y, minPos.z)
                    )
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IntersectQuad(float3 p1, float3 p2, float3 p3, float3 p4, Ray ray, out float distance)
        {
            if (IntersectTriangle(p1, p2, p4, ray, out distance)) return true;
            else if (IntersectTriangle(p3, p4, p2, ray, out distance)) return true;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Checks if the specified ray hits the triagnlge descibed by p1, p2 and p3.
        /// Möller–Trumbore ray-triangle intersection algorithm implementation.
        /// </summary>
        /// <param name="p1">Vertex 1 of the triangle.</param>
        /// <param name="p2">Vertex 2 of the triangle.</param>
        /// <param name="p3">Vertex 3 of the triangle.</param>
        /// <param name="ray">The ray to test hit for.</param>
        /// <returns><c>true</c> when the ray hits the triangle, otherwise <c>false</c></returns>
        private static bool IntersectTriangle(float3 p1, float3 p2, float3 p3, Ray ray, out float distance)
        {
            distance = 0;
            // Vectors from p1 to p2/p3 (edges)
            float3 e1, e2;

            float3 p, q, t;
            float det, invDet, u, v;

            //Find vectors for two edges sharing vertex/point p1
            e1 = p2 - p1;
            e2 = p3 - p1;

            // calculating determinant 
            p = math.cross(ray.direction, e2);

            //Calculate determinat
            det = math.dot(e1, p);
            
            //if determinant is near zero, ray lies in plane of triangle otherwise not
            if (det > -math.EPSILON && det < math.EPSILON) { return false; }
            invDet = 1.0f / det;

            //calculate distance from p1 to ray origin
            t = ((float3)ray.origin) - p1;

            //Calculate u parameter
            u = Vector3.Dot(t, p) * invDet;

            //Check for ray hit
            if (u < 0 || u > 1) { return false; }

            //Prepare to test v parameter
            q = math.cross(t, e1);

            //Calculate v parameter
            v = math.dot(ray.direction, q) * invDet;

            //Check for ray hit
            if (v < 0 || u + v > 1) { return false; }

            distance = (math.dot(e2, q) * invDet);
            if (distance > math.EPSILON)
            {
                //ray does intersect
                return true;
            }

            // No hit at all
            return false;
        }

        public bool Equals(AABB other)
        {
            return m_Center.Equals(other.m_Center) && m_Extents.Equals(other.m_Extents);
        }
        public bool IsZero() => Equals(Zero);

        public static implicit operator AABB(Bounds a) => new AABB(a.center, a.size);
        public static implicit operator Bounds(AABB a) => new Bounds(a.center, a.size);

        public struct Planes : IFixedList<Plane>
        {
            public Plane
                a0, a1, a2, a3, a4, a5;

            public Plane this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => a0,
                        1 => a1,
                        2 => a2,
                        3 => a3,
                        4 => a4,
                        5 => a5,
                        _ => throw new IndexOutOfRangeException(),
                    };
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            a0 = value;
                            break;
                        case 1:
                            a1 = value;
                            break;
                        case 2:
                            a2 = value;
                            break;
                        case 3:
                            a3 = value;
                            break;
                        case 4:
                            a4 = value;
                            break;
                        case 5:
                            a5 = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
            public Plane First => a0;
            public Plane Last => a5;

            public int Length => 6;
            public int Capacity { get => 6; set => throw new NotImplementedException(); }
            public bool IsEmpty => false;

            int IIndexable<Plane>.Length { get => 6; set => throw new NotImplementedException(); }

            void INativeList<Plane>.Clear()
            {
                throw new NotImplementedException();
            }
            ref Plane IIndexable<Plane>.ElementAt(int index)
            {
                throw new NotImplementedException();
            }
        }
        [BurstCompatible]
        public struct Vertices : IFixedList<float3>
        {
            public float3
                a0, a1, a2, a3,
                b0, b1, b2, b3;

            public float3 this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => a0,
                        1 => a1,
                        2 => a2,
                        3 => a3,
                        4 => b0,
                        5 => b1,
                        6 => b2,
                        7 => b3,
                        _ => throw new IndexOutOfRangeException(),
                    };
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public float3 First => a0;
            public float3 Last => b3;

            public int Length => 8;
            public int Capacity { get => 8; set => throw new NotImplementedException(); }
            public bool IsEmpty => false;

            int IIndexable<float3>.Length { get => 8; set => throw new NotImplementedException(); }

            void INativeList<float3>.Clear()
            {
                throw new NotImplementedException();
            }
            ref float3 IIndexable<float3>.ElementAt(int index)
            {
                throw new NotImplementedException();
            }
        }
    }
}
