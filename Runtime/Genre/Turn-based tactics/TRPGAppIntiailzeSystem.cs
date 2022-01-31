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


namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGAppIntiailzeSystem : PresentationSystemEntity<TRPGAppIntiailzeSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private bool m_IngameLayerStarted = false;

        private SceneSystem m_SceneSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);

            return base.OnInitialize();
        }
        protected override PresentationResult OnStartPresentation()
        {
            CheckCurrentSceneAndExecute();

            return base.OnStartPresentation();
        }
        protected override void OnDispose()
        {
            m_SceneSystem.OnSceneChanged -= CheckCurrentSceneAndExecute;

            m_SceneSystem = null;
        }

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;

            m_SceneSystem.OnSceneChanged += CheckCurrentSceneAndExecute;
        }

        private void CheckCurrentSceneAndExecute()
        {
            if (!m_IngameLayerStarted)
            {
                if ((!m_SceneSystem.IsMasterScene && !m_SceneSystem.IsStartScene)
                    || m_SceneSystem.IsDebugScene)
                {
                    PresentationSystemGroup<TRPGIngameSystemGroup>.Start();
                    "start ingame layer".ToLog();
                    m_IngameLayerStarted = true;
                }
            }
            else
            {
                if (m_SceneSystem.IsMasterScene || m_SceneSystem.IsStartScene)
                {
                    PresentationSystemGroup<TRPGIngameSystemGroup>.Stop();
                    "stop ingame layer".ToLog();
                    m_IngameLayerStarted = false;
                }
            }
        }
    }
}