// Copyright 2022 Seung Ha Kim
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

using Syadeu.Collections;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Render;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorStatusModule : PresentationSystemModule<ActorSystem>
    {
        private InstanceID m_CurrentActor = InstanceID.Empty;
        private UIDocument m_UIDocument;

        public InstanceID CurrentActor => m_CurrentActor;
        public bool IsOpened => m_UIDocument != null;

        public void EnableShortStatusUI(InstanceID actor)
        {
            if (!actor.IsEntity<IEntity>() || !actor.HasComponent<ActorStatComponent>())
            {
                "??".ToLogError();
                return;
            }
            ActorStatAttribute statAttribute = actor.GetEntity<IEntity>().GetAttribute<ActorStatAttribute>();

            m_UIDocument = statAttribute.ShortUI;

            //setting

            //
            m_UIDocument.SetActive(true);
        }
        public void DisableShortStatusUI()
        {
            if (m_UIDocument == null) return;

            m_UIDocument.SetActive(false);
            m_UIDocument = null;
        }
    }
}
