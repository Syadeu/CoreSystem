using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class ActionBase : ObjectBase
    {
        private static PresentationSystemID<EntitySystem> m_EntitySystem = PresentationSystemID<EntitySystem>.Null;

        [Header("Debug")]
        [JsonProperty(Order = 9999, PropertyName = "DebugText")]
        protected string p_DebugText = string.Empty;

        [JsonIgnore] private bool m_Terminated = true;
        [JsonIgnore] public FixedReference m_Reference;

        [JsonIgnore] public bool Terminated => m_Terminated;

        protected static bool TryGetEntitySystem(out EntitySystem entitySystem)
        {
            if (m_EntitySystem.IsNull())
            {
                m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
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

        public override sealed object Clone()
        {
            ActionBase actionBase = (ActionBase)base.Clone();

            actionBase.p_DebugText = string.Copy(p_DebugText);

            return actionBase;
        }
        public override sealed string ToString() => Name;
        public override sealed bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
