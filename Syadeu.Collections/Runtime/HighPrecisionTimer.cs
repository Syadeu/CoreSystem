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

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="Int64"/> 까지 지원하는 "나름" 정확도가 높은 타이머입니다.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public struct HighPrecisionTimer
    {
        private long m_TimeStamp;

        public void Reset() => m_TimeStamp = 0;
        public void Begin() => m_TimeStamp = HighPrecisionTime.Now;
        /// <summary>
        /// 
        /// </summary>
        /// <returns>eclapsed time</returns>
        public TimeSpan End()
        {
            TimeSpan span = HighPrecisionTime.CalculateDelta(m_TimeStamp, HighPrecisionTime.Now);
            return span;
        }
    }
}
