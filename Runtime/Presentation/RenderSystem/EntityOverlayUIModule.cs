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

#if CORESYSTEM_DOTWEEN
using DG.Tweening;
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Unity.Collections;

namespace Syadeu.Presentation.Render
{
    public sealed class EntityOverlayUIModule : PresentationSystemModule<WorldCanvasSystem>
    {
        private NativeMultiHashMap<InstanceID, Entity<UIObjectEntity>> m_OverlayHashMap;

        private EntitySystem m_EntitySystem;

        protected override void OnInitialize()
        {
            m_OverlayHashMap = new NativeMultiHashMap<InstanceID, Entity<UIObjectEntity>>(1024, AllocatorManager.Persistent);

            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
        }

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
        }
        private void M_EntitySystem_OnEntityDestroy(IObject obj)
        {
            if (!m_OverlayHashMap.ContainsKey(obj.Idx)) return;

            UnregisterOverlayUI(obj.Idx);
        }

        protected override void OnShutDown()
        {
            base.OnShutDown();
        }
        protected override void OnDispose()
        {
            m_OverlayHashMap.Dispose();

            m_EntitySystem = null;
        }

        protected override void AfterTransformPresentation()
        {

        }

        public void UnregisterOverlayUI(InstanceID instance)
        {
            if (!m_OverlayHashMap.ContainsKey(instance))
            {
                return;
            }

            m_OverlayHashMap.TryGetFirstValue(instance, out Entity<UIObjectEntity> entity, out NativeMultiHashMapIterator<InstanceID> iter);
            do
            {
                m_EntitySystem.DestroyEntity(entity);
            } while (m_OverlayHashMap.TryGetNextValue(out entity, ref iter));

            m_OverlayHashMap.Remove(instance);
        }
    }
}
