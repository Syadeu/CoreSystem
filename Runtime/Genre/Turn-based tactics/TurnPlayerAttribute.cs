﻿using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: Turn Player")]
    [AttributeAcceptOnly(typeof(ActorEntity))]
    public sealed class TurnPlayerAttribute : AttributeBase,
        INotifyComponent<TurnPlayerComponent>
    {
        [Header("Generals")]
        [JsonProperty(Order = 0, PropertyName = "ActivateOnCreate")] internal bool m_ActivateOnCreate = true;
        [JsonProperty(Order = 1, PropertyName = "TurnSpeed")] internal float m_TurnSpeed = 0;
        [JsonProperty(Order = 2, PropertyName = "MaxActionPoint")] internal int m_MaxActionPoint = 6;

        [Space]
        [Header("Actions")]
        [JsonProperty(Order = 3, PropertyName = "OnStartTurn")]
        internal Reference<TriggerAction>[] m_OnStartTurnActions = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 4, PropertyName = "OnEndTurn")]
        internal Reference<TriggerAction>[] m_OnEndTurnActions = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnResetTurn")]
        internal Reference<TriggerAction>[] m_OnResetTurnActions = Array.Empty<Reference<TriggerAction>>();
    }
    [Preserve]
    internal sealed class TurnPlayerProcessor : AttributeProcessor<TurnPlayerAttribute>
    {
        private TRPGTurnTableSystem m_TurnTableSystem;

        protected override void OnInitialize()
        {
            RequestSystem<TRPGSystemGroup, TRPGTurnTableSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_TurnTableSystem = null;
        }

        #region Binds

        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }

        #endregion

        protected override void OnCreated(TurnPlayerAttribute attribute, EntityData<IEntityData> entity)
        {
            TurnPlayerComponent component = new TurnPlayerComponent(attribute, EntitySystem.CreateHashCode());
            component = entity.AddComponent(in component);

            m_TurnTableSystem.AddPlayer(entity);
        }
        protected override void OnDestroy(TurnPlayerAttribute attribute, EntityData<IEntityData> entity)
        {
            m_TurnTableSystem.RemovePlayer(entity);
        }
    }
}
