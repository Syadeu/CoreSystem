using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: TRPG Selection")]
    [AttributeAcceptOnly(typeof(ActorEntity), typeof(ObjectEntity))]
    public sealed class TRPGSelectionAttribute : AttributeBase,
        INotifyComponent<TRPGSelectionComponent>
    {
        [JsonProperty(Order = 0, PropertyName = "SelectedFloorUI")]
        internal FXBounds[] m_SelectedFloorUI = Array.Empty<FXBounds>();

        [JsonProperty]
        internal Reference<EntityDataBase>[] m_CreateEntityOnSelect = Array.Empty<Reference<EntityDataBase>>();
    }
    internal sealed class TRPGSelectionProcessor : AttributeProcessor<TRPGSelectionAttribute>
    {
        protected override void OnCreated(TRPGSelectionAttribute attribute, EntityData<IEntityData> entity)
        {
            ref var com = ref entity.GetComponent<TRPGSelectionComponent>();

            com = new TRPGSelectionComponent()
            {
                m_Selected = false
            };

            for (int i = 0; i < attribute.m_CreateEntityOnSelect.Length; i++)
            {
                EntitySystem.CreateObject(attribute.m_CreateEntityOnSelect[i]);
            }
        }
    }
}