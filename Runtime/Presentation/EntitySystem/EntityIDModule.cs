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

using Syadeu.Collections;
using Unity.Collections;

namespace Syadeu.Presentation
{
    internal sealed class EntityIDModule : PresentationSystemModule<EntitySystem>
    {
        private NativeHashMap<EntityShortID, InstanceID> m_EntityConversions;
        private NativeHashMap<InstanceID, EntityShortID> m_EntityShortConversions;

        protected override void OnInitialize()
        {
            m_EntityConversions = new NativeHashMap<EntityShortID, InstanceID>(1024, AllocatorManager.Persistent);
            m_EntityShortConversions = new NativeHashMap<InstanceID, EntityShortID>(1024, AllocatorManager.Persistent);

            //System.OnEntityDestroy += System_OnEntityDestroy;
        }
        //private void System_OnEntityDestroy(IEntityData obj)
        //{
        //    if (!m_EntityShortConversions.IsCreated)
        //    {
        //        return;
        //    }

        //    EntityID id = obj.Idx;
        //    if (m_EntityShortConversions.TryGetValue(id, out EntityShortID shortID))
        //    {
        //        m_EntityConversions.Remove(shortID);
        //        m_EntityShortConversions.Remove(id);
        //    }
        //}
        protected override void OnDispose()
        {
            m_EntityConversions.Dispose();
            m_EntityShortConversions.Dispose();
        }

        public EntityShortID Convert(InstanceID id)
        {
            EntityShortID shortID = new EntityShortID(id);
            if (m_EntityConversions.TryGetValue(shortID, out InstanceID exist))
            {
                if (!exist.Equals(id))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"id conflect.");
                }

                return shortID;
            }

            m_EntityConversions.Add(shortID, id);
            m_EntityShortConversions.Add(id, shortID);

            return shortID;
        }
        public InstanceID Convert(EntityShortID id)
        {
            return m_EntityConversions[id];
        }
    }

    internal sealed class EntityTransformModule : PresentationSystemModule<EntitySystem>
    {

    }
}
