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
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Syadeu.Presentation.Timeline
{
    public sealed class AnimatorTriggerMarker : Marker, INotification
    {
        [SerializeField] private string m_TriggerString = string.Empty;

        private PropertyName m_ID = default(PropertyName);
        public PropertyName id
        {
            get
            {
                if (PropertyName.IsNullOrEmpty(m_ID))
                {
                    m_ID = new PropertyName(GetHashCode());
                }
                return m_ID;
            }
        }

        private int m_TriggerKey = 0;
        public int TriggerKey
        {
            get
            {
                if (!string.IsNullOrEmpty(m_TriggerString) && m_TriggerKey == 0)
                {
                    m_TriggerKey = Animator.StringToHash(m_TriggerString);
                }
                return m_TriggerKey;
            }
        }
    }
}
