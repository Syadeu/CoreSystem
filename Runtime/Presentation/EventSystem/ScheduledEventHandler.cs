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

using Syadeu.Collections;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation.Events
{
    public sealed class ScheduledEventHandler
    {
        internal ISystemEventScheduler m_System;
        private float m_EnteredTime;

        internal SystemEventResult m_Result;
        internal Type m_EventType;

        internal void NotifyEnteringAwait(ISystemEventScheduler system)
        {
            m_System = system;
            m_EnteredTime = CoreSystem.time;
        }
        internal void Reset()
        {
            m_System = null;
            m_EnteredTime = -1;

            m_Result = SystemEventResult.Success;
            m_EventType = null;
        }
        internal bool IsExceedTimeout(float timeout)
        {
            if (CoreSystem.time - m_EnteredTime < timeout) return true;
            return false;
        }

        public void SetEvent<T>(SystemEventResult result)
        {
            m_Result = result;
            m_EventType = TypeHelper.TypeOf<T>.Type;
        }
        public void SetEvent(SystemEventResult result, Type eventType)
        {
            m_Result = result;
            m_EventType = eventType;
        }
    }
}
