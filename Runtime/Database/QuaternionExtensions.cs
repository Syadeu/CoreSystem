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

using Unity.Mathematics;

namespace Syadeu
{
    public static class QuaternionExtensions
    {
        public static float ComputeXAngle(this quaternion q)
        {
            float sinr_cosp = 2 * (q.value.w * q.value.x + q.value.y * q.value.z);
            float cosr_cosp = 1 - 2 * (q.value.x * q.value.x + q.value.y * q.value.y);
            return math.atan2(sinr_cosp, cosr_cosp);
        }

        public static float ComputeYAngle(this quaternion q)
        {
            float sinp = 2 * (q.value.w * q.value.y - q.value.z * q.value.x);
            if (math.abs(sinp) >= 1)
                return math.PI / 2 * math.sign(sinp); // use 90 degrees if out of range
            else
                return math.asin(sinp);
        }

        public static float ComputeZAngle(this quaternion q)
        {
            float siny_cosp = 2 * (q.value.w * q.value.z + q.value.x * q.value.y);
            float cosy_cosp = 1 - 2 * (q.value.y * q.value.y + q.value.z * q.value.z);
            return math.atan2(siny_cosp, cosy_cosp);
        }

        public static float3 Euler(this quaternion q)
        {
            return new float3(ComputeXAngle(q), ComputeYAngle(q), ComputeZAngle(q));
        }

        public static quaternion FromAngles(float3 angles)
        {

            float cy = math.cos(angles.z * 0.5f);
            float sy = math.sin(angles.z * 0.5f);
            float cp = math.cos(angles.y * 0.5f);
            float sp = math.sin(angles.y * 0.5f);
            float cr = math.cos(angles.x * 0.5f);
            float sr = math.sin(angles.x * 0.5f);

            float4 q;
            q.w = cr * cp * cy + sr * sp * sy;
            q.x = sr * cp * cy - cr * sp * sy;
            q.y = cr * sp * cy + sr * cp * sy;
            q.z = cr * cp * sy - sr * sp * cy;

            return q;

        }
    }
}