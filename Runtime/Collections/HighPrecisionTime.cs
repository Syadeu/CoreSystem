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
using System.Diagnostics;

namespace Syadeu.Collections
{
    public static class HighPrecisionTime
    {
        public static long Now => Stopwatch.GetTimestamp();

        public static long CalculateTimeAtDelta(long from, TimeSpan delta)
        {
            return from + (long)(delta.TotalSeconds * Stopwatch.Frequency);
        }
        public static TimeSpan CalculateDelta(long from, long to)
        {
            return TimeSpan.FromTicks((long)(((double)(to - from) / Stopwatch.Frequency) * TimeSpan.TicksPerSecond));
        }
    }
    public static class TimeSpanExtensions
    {
        private const long c_TicksPerMicrosecond = (TimeSpan.TicksPerSecond / 1000000L);

        /// <summary>
        /// Gets the microseconds component of the time interval represented by the current System.TimeSpan structure.
        /// </summary>
        /// <param name="span"></param>
        /// <returns>The microsecond component of the current System.TimeSpan structure. The return value ranges from -999 through 999.</returns>
        public static int Microseconds(this TimeSpan span)
        {
            return (int)(span.Ticks / c_TicksPerMicrosecond % 1000L);
        }

        /// <summary>
        /// Gets the value of the current System.TimeSpan structure expressed in whole and fractional microseconds.
        /// </summary>
        /// <param name="span"></param>
        /// <returns>The total number of microseconds represented by this instance.</returns>
        public static double TotalMicroseconds(this TimeSpan span)
        {
            return ((double)span.Ticks / c_TicksPerMicrosecond);
        }
    }
}
