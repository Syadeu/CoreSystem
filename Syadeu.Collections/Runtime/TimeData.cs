﻿// Copyright 2022 Seung Ha Kim
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

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="TimeSpan"/> 의 Serialized 버전
    /// </summary>
    [Serializable]
    public struct TimeData
    {
        public static TimeData Current => new TimeData(DateTime.UtcNow.TimeOfDay);

        [UnityEngine.SerializeField]
        private int3 m_Time;

        public TimeSpan Time => new TimeSpan(m_Time.x, m_Time.y, m_Time.z);

        public TimeData(int hours, int minutes, int seconds)
        {
            m_Time = new int3(hours, minutes, seconds);
        }
        public TimeData(int3 time)
        {
            m_Time = new int3(time.x, time.y, time.z);
        }
        public TimeData(TimeSpan time)
        {
            m_Time = new int3(time.Hours, time.Minutes, time.Seconds);
        }

        public static TimeData operator +(TimeData x, TimeData y)
        {
            return new TimeData(TimeSpan.FromTicks(x.Time.Ticks + y.Time.Ticks));
        }
        public static TimeData operator-(TimeData x, TimeData y)
        {
            return new TimeData(TimeSpan.FromTicks(x.Time.Ticks - y.Time.Ticks));
        }
    }
}
