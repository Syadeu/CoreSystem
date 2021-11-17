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
using UnityEngine;

namespace Syadeu
{
    /// <summary>
    /// 메인 스레드잡이 끝날때까지 기다릴수있는 클래스입니다
    /// </summary>
    public sealed class WaitForForegroundJob : CustomYieldInstruction
    {
        private readonly ForegroundJob Job;

        internal string CalledFrom { get; } = null;
        /// <summary>
        /// 잡을 넣어주세요
        /// </summary>
        /// <param name="job"></param>
        public WaitForForegroundJob(ForegroundJob job)
        {
            Job = job;
            CalledFrom = Environment.StackTrace;
        }

        public override bool keepWaiting
        {
            get
            {
                if (Job == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "Null 인 잡은 기다릴수 없습니다", CalledFrom);
                }
                if (Job.Faild)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "잡을 실행하는 도중 에러가 발생되었습니다", Job.CalledFrom);
                }

                if (!Job.IsDone) return true;
                return false;
            }
        }
    }
}
