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

namespace Syadeu.Collections
{
    [Serializable]
    public struct TimeData
    {
        public static TimeData Current => new TimeData(DateTime.UtcNow.TimeOfDay);

        private TimeSpan m_Time;

        public TimeSpan Time => m_Time;

        public TimeData(int hours, int minutes, int seconds)
        {
            m_Time = new TimeSpan(hours, minutes, seconds);
        }
        public TimeData(int3 time)
        {
            m_Time = new TimeSpan(time.x, time.y, time.z);
        }
        public TimeData(TimeSpan time)
        {
            m_Time = time;
        }

        public static TimeData operator +(TimeData x, TimeData y)
        {
            return new TimeData(TimeSpan.FromTicks(x.m_Time.Ticks + y.m_Time.Ticks));
        }
        public static TimeData operator-(TimeData x, TimeData y)
        {
            return new TimeData(TimeSpan.FromTicks(x.m_Time.Ticks - y.m_Time.Ticks));
        }
    }
}
