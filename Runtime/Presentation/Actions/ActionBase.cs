using Newtonsoft.Json;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public abstract class ActionBase : ObjectBase
    {
        private static PresentationSystemID<EntitySystem> m_EntitySystem = PresentationSystemID<EntitySystem>.Null;

        [JsonIgnore] private bool m_Terminated = true;
        [JsonIgnore] public Reference m_Reference;

        [JsonIgnore] public bool Terminated => m_Terminated;

        protected static bool TryGetEntitySystem(out EntitySystem entitySystem)
        {
            if (m_EntitySystem.IsNull())
            {
                m_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                if (m_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    entitySystem = null;
                    return false;
                }
            }
            entitySystem = m_EntitySystem.System;
            return true;
        }
        internal virtual void InternalInitialize()
        {
            m_Terminated = false;
        }
        internal virtual void InternalTerminate()
        {
            m_Terminated = true;
        }
        internal void InternalCreate()
        {
            OnCreated();
        }

        /// <summary>
        /// 객체가 처음 생성될떄 실행됩니다.
        /// </summary>
        protected virtual void OnCreated() { }
        /// <summary>
        /// 이 액션이 시스템에서 더 이상 사용하지 않을때 실행됩니다.
        /// </summary>
        protected override void OnDispose() { }

        public override sealed object Clone() => base.Clone();
        public override sealed int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override sealed string ToString() => Name;
        public override sealed bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
