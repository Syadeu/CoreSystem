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

using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Collections.LowLevel
{
    [BurstCompile(CompileSynchronously = true)]
    public unsafe static class BurstMathematics
    {
        [BurstCompile]
        public static bool IntersectQuad(
            in float3 p1, in float3 p2, in float3 p3, in float3 p4, 
            in Ray ray, float* distance)
        {
            if (IntersectTriangle(p1, p2, p4, ray, distance)) return true;
            else if (IntersectTriangle(p3, p4, p2, ray, distance)) return true;
            return false;
        }
        /// <summary>
        /// Checks if the specified ray hits the triagnlge descibed by p1, p2 and p3.
        /// Möller–Trumbore ray-triangle intersection algorithm implementation.
        /// </summary>
        /// <param name="p1">Vertex 1 of the triangle.</param>
        /// <param name="p2">Vertex 2 of the triangle.</param>
        /// <param name="p3">Vertex 3 of the triangle.</param>
        /// <param name="ray">The ray to test hit for.</param>
        /// <returns><c>true</c> when the ray hits the triangle, otherwise <c>false</c></returns>
        [BurstCompile]
        public static bool IntersectTriangle(
            in float3 p1, in float3 p2, in float3 p3, 
            in Ray ray, float* distance)
        {
            *distance = 0;
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

            *distance = (math.dot(e2, q) * invDet);
            if (*distance > math.EPSILON)
            {
                //ray does intersect
                return true;
            }

            // No hit at all
            return false;
        }
        [BurstCompile]
        public static void CalculateRotationWithVertices(
            in AABB aabb, in quaternion quaternion, in float3 scale, AABB* result)
        {
            float3 originCenter = aabb.center;
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

            *result = temp;
        }
    }
}
