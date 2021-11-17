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
    /// 등록한 인덱스의 백그라운드잡 워커의 작업을 기다릴수있는 클래스입니다
    /// </summary>
    public class WaitForBackgroundJobWorker : CustomYieldInstruction
    {
        private readonly CoreSystem.BackgroundJobWorker Worker;

        internal string CalledFrom { get; } = null;
        /// <summary>
        /// 백그라운드잡 워커의 인덱스를 넣어주세요
        /// </summary>
        /// <param name="workerIndex"></param>
        public WaitForBackgroundJobWorker(int workerIndex)
        {
            Worker = CoreSystem.Instance.GetBackgroundJobWorker(workerIndex);
            CalledFrom = Environment.StackTrace;
        }

        public override bool keepWaiting
        {
            get
            {
                if (Worker == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "Null 인 워커는 기다릴수 없습니다", CalledFrom);
                }

                if (Worker.Worker.IsBusy) return true;
                return false;
            }
        }
    }
}
