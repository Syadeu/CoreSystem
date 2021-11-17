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
using UnityEngine.UI;

namespace Syadeu.Presentation
{
    public class CustomLoadingScene : LoadingSceneSetupEntity
    {
        [SerializeField] protected Camera m_Camera;
        [SerializeField] protected CanvasGroup m_FadeGroup;

        protected override void Start()
        {
            //if (m_Camera == null) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
            //    "Camera 는 null 이 될 수 없습니다.");
            if (m_FadeGroup == null) throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                "FadeGroup 은 null 이 될 수 없습니다.");

            m_OnLoadingEnter.AddListener(() =>
            {
                m_FadeGroup.interactable = true;
                m_FadeGroup.blocksRaycasts = true;
                m_FadeGroup.Lerp(1, Time.deltaTime * 2);
            });
            m_OnLoadingExit.AddListener(() =>
            {
                m_FadeGroup.interactable = false;
                m_FadeGroup.blocksRaycasts = false;
                m_FadeGroup.Lerp(0, Time.deltaTime * 2);
            });

            Initialize();
        }
    }
}
