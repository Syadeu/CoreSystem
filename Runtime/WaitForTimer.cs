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

using UnityEngine;

namespace Syadeu
{
    public sealed class WaitForTimer : CustomYieldInstruction
    {
        private readonly Timer Timer;

        public WaitForTimer(Timer timer)
        {
            Timer = timer;
        }

        public override bool keepWaiting
        {
            get
            {
                if (Timer == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Jobs, "Null 인 타이머는 기다릴수 없습니다");
                }

                if (Timer.Killed || Timer.Completed || Timer.Disposed)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
