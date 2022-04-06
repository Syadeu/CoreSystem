// Copyright 2022 Seung Ha Kim
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
    public struct Timer
    {
        private float m_StartTime;

        public static Timer Start()
        {
            Timer timer = new Timer
            {
                m_StartTime = Time.time,
            };
            return timer;
        }

        public void Reset()
        {
            m_StartTime = Time.time;
        }
        public float ElapsedTime
        {
            get
            {
                return Time.time - m_StartTime;
            }
        }

        public bool IsExceeded(int3 time)
        {
            float secs = (time.x * 3600) + (time.y * 60) + time.z;
            return secs < ElapsedTime;
        }
    }
}
