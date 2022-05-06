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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Proxy
{
    [BurstCompatible]
    public struct MessageContext
    {
        private FixedString512Bytes m_MethodName;
        private int m_UserData;
        private SendMessageOptions m_Options;

        public string methodName
        {
            get => m_MethodName.ToString();
            set => m_MethodName = value;
        }
        public int UserData
        {
            get => m_UserData;
            set => m_UserData = value;
        }
        public SendMessageOptions options
        {
            get => m_Options;
            set => m_Options = value;
        }

        [NotBurstCompatible]
        public MessageContext(string methodName, SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            m_MethodName = methodName;
            m_UserData = 0;
            m_Options = options;
        }
        [NotBurstCompatible]
        public MessageContext(string methodName, int userData, SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            m_MethodName = methodName;
            m_UserData = userData;
            m_Options = options;
        }

        public override int GetHashCode()
        {
            return unchecked(m_MethodName.GetHashCode() ^ (int)m_Options);
        }
    }
}
