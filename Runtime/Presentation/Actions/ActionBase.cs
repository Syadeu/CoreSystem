using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    public abstract class ActionBase : ObjectBase
    {
        [Header("Debug")]
        [JsonProperty(Order = 9999, PropertyName = "DebugText")]
        protected string p_DebugText = string.Empty;

        [JsonIgnore] public IFixedReference m_Reference;

        protected static bool TryGetEntitySystem(out EntitySystem entitySystem)
        {
            entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;
            return true;
        }

        public override sealed object Clone()
        {
            ActionBase actionBase = (ActionBase)base.Clone();

            actionBase.p_DebugText = string.Copy(p_DebugText);

            return actionBase;
        }
        public override sealed string ToString() => Name;
    }
}
