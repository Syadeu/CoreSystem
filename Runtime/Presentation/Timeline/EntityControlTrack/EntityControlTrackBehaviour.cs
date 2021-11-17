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

using Syadeu.Presentation.Attributes;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Syadeu.Presentation.Timeline
{
    [Serializable]
    public class EntityControlTrackBehaviour : PlayableBehaviour
    {
        //private AnimatorComponent m_Animator;

        public override void OnPlayableCreate(Playable playable)
        {
        }
        public void Initialize(/*AnimatorComponent animator*/)
        {
            //m_Animator = animator;
        }
    }
}
