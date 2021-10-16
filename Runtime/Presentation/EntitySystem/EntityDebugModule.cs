#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;

namespace Syadeu.Presentation
{
    internal sealed class EntityDebugModule : PresentationSystemModule<EntitySystem>
    {
#if DEBUG_MODE
        protected override void OnInitialize()
        {
            PoolContainer<ComponentAtomicSafetyHandler>.Initialize(SafetyFactory, 32);
        }
        private ComponentAtomicSafetyHandler SafetyFactory()
        {
            return new ComponentAtomicSafetyHandler(System);
        }

        public void CheckAllComponentIsDisposed(ObjectBase obj, float delayed = 1f)
        {
            var handler = PoolContainer<ComponentAtomicSafetyHandler>.Dequeue();
            handler.Initialize(obj);
            CoreSystem.WaitInvokeBackground(delayed, handler.CheckAllComponentIsDisposed);
        }

        private bool Debug_HasComponent(InstanceID entity, out int count, out string names)
        {
            if (System.m_AddedComponents.TryGetValue(entity, out var list))
            {
                count = list.Count;
                names = list[0].Name;
                for (int i = 1; i < list.Count; i++)
                {
                    names += $", {list[i].Name}";
                }

                return true;
            }

            count = 0;
            names = string.Empty;
            return false;
        }

        private sealed class ComponentAtomicSafetyHandler
        {
            private const string
                c_ComponentIsNotFullDisposed
                    = "Entity({0}) has number of {1} components that didn\'t disposed. {2}",
                c_ComponentFullDiposed
                    = "Entity({0}) component all checked.";

            private EntitySystem m_EntitySystem;

            private string m_Name;
            private InstanceID m_InstanceID;

            public ComponentAtomicSafetyHandler(EntitySystem entitySystem)
            {
                m_EntitySystem = entitySystem;
            }

            public void Initialize(ObjectBase obj)
            {
                m_Name = obj.Name;
                m_InstanceID = obj.Idx;
            }

            public void CheckAllComponentIsDisposed()
            {
                if (Debug_HasComponent(m_InstanceID, out int count, out string names))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        string.Format(c_ComponentIsNotFullDisposed, m_Name, count, names));
                }
                else
                {
                    CoreSystem.Logger.Log(Channel.Entity,
                        string.Format(c_ComponentFullDiposed, m_Name));
                }

                m_Name = null;
                PoolContainer<ComponentAtomicSafetyHandler>.Enqueue(this);
            }

            private bool Debug_HasComponent(InstanceID entity, out int count, out string names)
            {
                if (m_EntitySystem.m_AddedComponents.TryGetValue(entity, out var list))
                {
                    count = list.Count;
                    names = list[0].Name;
                    for (int i = 1; i < list.Count; i++)
                    {
                        names += $", {list[i].Name}";
                    }

                    return true;
                }

                count = 0;
                names = string.Empty;
                return false;
            }
        }
#endif
    }
}
