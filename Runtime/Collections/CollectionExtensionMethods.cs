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
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Collections
{
    public static class CollectionExtensionMethods
    {
        public static TypeInfo ToTypeInfo(this Type t) => CollectionUtility.GetTypeInfo(t);

        public static Direction GetOpposite(this Direction t)
        {
            Direction result = Direction.NONE;

            if ((t & Direction.Left) == Direction.Left)
            {
                result |= Direction.Right;
            }
            if ((t & Direction.Right) == Direction.Right)
            {
                result |= Direction.Left;
            }
            if ((t & Direction.Up) == Direction.Up)
            {
                result |= Direction.Down;
            }
            if ((t & Direction.Down) == Direction.Down)
            {
                result |= Direction.Up;
            }
            if ((t & Direction.Forward) == Direction.Forward)
            {
                result |= Direction.Backward;
            }
            if ((t & Direction.Backward) == Direction.Backward)
            {
                result |= Direction.Forward;
            }

            return result;
        }
        public static int ToIndex(this Direction t)
        {
            if ((t & Direction.Up) == Direction.Left)
            {
                return 0;
            }
            if ((t & Direction.Down) == Direction.Right)
            {
                return 1;
            }
            if ((t & Direction.Left) == Direction.Up)
            {
                return 2;
            }
            if ((t & Direction.Right) == Direction.Down)
            {
                return 3;
            }
            if ((t & Direction.Forward) == Direction.Forward)
            {
                return 4;
            }
            if ((t & Direction.Backward) == Direction.Backward)
            {
                return 5;
            }

            return -1;
        }

        public static bool IsNullOrEmpty(this string t) => string.IsNullOrEmpty(t);

        #region Matrix

        #region Copyright 2020-2022 Andreas Atteneder
        // https://gist.github.com/atteneder/b6675c9a73860c00d795dcea7149e8d2
        //
        // Licensed under the Apache License, Version 2.0 (the "License");
        // you may not use this file except in compliance with the License.
        // You may obtain a copy of the License at
        //
        //      http://www.apache.org/licenses/LICENSE-2.0
        //
        // Unless required by applicable law or agreed to in writing, software
        // distributed under the License is distributed on an "AS IS" BASIS,
        // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        // See the License for the specific language governing permissions and
        // limitations under the License.

        /// <summary>
        /// Decomposes a 4x4 TRS matrix into separate transforms (translation * rotation * scale)
        /// Matrix may not contain skew
        /// </summary>
        /// <param name="translation">Translation</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="scale">Scale</param>
        public static void Decompose(
            this Matrix4x4 m,
            out Vector3 translation,
            out Quaternion rotation,
            out Vector3 scale
            )
        {
            translation = new Vector3(m.m03, m.m13, m.m23);
            var mRotScale = new float3x3(
                m.m00, m.m01, m.m02,
                m.m10, m.m11, m.m12,
                m.m20, m.m21, m.m22
                );
            mRotScale.Decompose(out float4 mRotation, out float3 mScale);
            rotation = new Quaternion(mRotation.x, mRotation.y, mRotation.z, mRotation.w);
            scale = new Vector3(mScale.x, mScale.y, mScale.z);
        }

        /// <summary>
        /// Decomposes a 4x4 TRS matrix into separate transforms (translation * rotation * scale)
        /// Matrix may not contain skew
        /// </summary>
        /// <param name="translation">Translation</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="scale">Scale</param>
        public static void Decompose(
            this float4x4 m,
            out float3 translation,
            out float4 rotation,
            out float3 scale
            )
        {
            var mRotScale = new float3x3(
                m.c0.xyz,
                m.c1.xyz,
                m.c2.xyz
                );
            mRotScale.Decompose(out rotation, out scale);
            translation = m.c3.xyz;
        }

        /// <summary>
        /// Decomposes a 3x3 matrix into rotation and scale
        /// </summary>
        /// <param name="rotation">Rotation quaternion values</param>
        /// <param name="scale">Scale</param>
        public static void Decompose(this float3x3 m, out float4 rotation, out float3 scale)
        {
            var lenC0 = math.length(m.c0);
            var lenC1 = math.length(m.c1);
            var lenC2 = math.length(m.c2);

            float3x3 rotationMatrix;
            rotationMatrix.c0 = m.c0 / lenC0;
            rotationMatrix.c1 = m.c1 / lenC1;
            rotationMatrix.c2 = m.c2 / lenC2;

            scale.x = lenC0;
            scale.y = lenC1;
            scale.z = lenC2;

            if (rotationMatrix.IsNegative())
            {
                rotationMatrix *= -1f;
                scale *= -1f;
            }

            // Inlined normalize(rotationMatrix)
            rotationMatrix.c0 = math.normalize(rotationMatrix.c0);
            rotationMatrix.c1 = math.normalize(rotationMatrix.c1);
            rotationMatrix.c2 = math.normalize(rotationMatrix.c2);

            rotation = new quaternion(rotationMatrix).value;
        }

        static float normalize(float3 input, out float3 output)
        {
            float len = math.length(input);
            output = input / len;
            return len;
        }

        static void normalize(ref float3x3 m)
        {
            m.c0 = math.normalize(m.c0);
            m.c1 = math.normalize(m.c1);
            m.c2 = math.normalize(m.c2);
        }

        static bool IsNegative(this float3x3 m)
        {
            var cross = math.cross(m.c0, m.c1);
            return math.dot(cross, m.c2) < 0f;
        }

        #endregion

        #endregion

        public static TComponent GetOrAddComponent<TComponent>(this GameObject t)
            where TComponent : Component
        {
            TComponent component = t.GetComponent<TComponent>();
            if (component == null)
            {
                component = t.AddComponent<TComponent>();
            }

            return component;
        }
    }
}
