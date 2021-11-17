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

#undef UNITY_ADDRESSABLES

using UnityEngine;

#if UNITY_EDITOR
#endif

namespace Syadeu.Presentation
{
    public sealed class SceneUtilityComponent : MonoBehaviour
    {
        [SerializeField] private int m_SceneIndex;
        [SerializeField] private float m_PreDelay;
        [SerializeField] private float m_PostDelay;

        public void LoadStartScene(float preDelay, float postDelay)
        {
            PresentationSystem<DefaultPresentationGroup, SceneSystem>
                .System
                .LoadStartScene(preDelay, postDelay);
        }
        public void LoadScene(int index, float preDelay, float postDelay)
        {
            PresentationSystem<DefaultPresentationGroup, SceneSystem>
                .System
                .LoadScene(index, preDelay, postDelay);
        }

        public void LoadStartScene() => LoadStartScene(m_PreDelay, m_PostDelay);
        public void LoadScene() => LoadScene(m_SceneIndex, m_PreDelay, m_PostDelay);
    }
}
