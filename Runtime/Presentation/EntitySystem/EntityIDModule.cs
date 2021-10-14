#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Unity.Collections;

namespace Syadeu.Presentation
{
    internal sealed class EntityIDModule : PresentationSystemModule<EntitySystem>
    {
        private NativeHashMap<EntityShortID, EntityID> m_EntityConversions;
        private NativeHashMap<EntityID, EntityShortID> m_EntityShortConversions;

        protected override void OnInitialize()
        {
            m_EntityConversions = new NativeHashMap<EntityShortID, EntityID>(1024, AllocatorManager.Persistent);
            m_EntityShortConversions = new NativeHashMap<EntityID, EntityShortID>(1024, AllocatorManager.Persistent);

            System.OnEntityDestroy += System_OnEntityDestroy;
        }
        private void System_OnEntityDestroy(IEntityData obj)
        {
            if (!m_EntityShortConversions.IsCreated)
            {
                return;
            }

            EntityID id = obj.Idx;
            if (m_EntityShortConversions.TryGetValue(id, out EntityShortID shortID))
            {
                m_EntityConversions.Remove(shortID);
                m_EntityShortConversions.Remove(id);
            }
        }
        protected override void OnDispose()
        {
            m_EntityConversions.Dispose();
            m_EntityShortConversions.Dispose();
        }

        public EntityShortID Convert(EntityID id)
        {
            EntityShortID shortID = new EntityShortID(id);
            if (m_EntityConversions.TryGetValue(shortID, out EntityID exist))
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

            if (!m_EntityConversions.ContainsKey(shortID))
            {
                "??".ToLogError();
            }
            else "converted".ToLog();

            return shortID;
        }
        public EntityID Convert(EntityShortID id)
        {
            return m_EntityConversions[id];
        }
    }
}
