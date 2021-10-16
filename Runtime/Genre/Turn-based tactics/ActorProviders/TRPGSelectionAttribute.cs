using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.TurnTable
{
    [AttributeAcceptOnly(typeof(ActorEntity), typeof(ObjectEntity))]
    public sealed class TRPGSelectionAttribute : AttributeBase,
        INotifyComponent<TRPGSelectionComponent>
    {
        [JsonProperty(Order = 0, PropertyName = "SelectedFloorUI")]
        internal FXBounds[] m_SelectedFloorUI = Array.Empty<FXBounds>();
    }
    internal sealed class TRPGSelectionProcessor : AttributeProcessor<TRPGSelectionAttribute>
    {
        protected override void OnCreated(TRPGSelectionAttribute attribute, EntityData<IEntityData> entity)
        {
            entity.AddComponent<TRPGSelectionComponent>();
            ref var com = ref entity.GetComponent<TRPGSelectionComponent>();

            com = new TRPGSelectionComponent()
            {
                m_Selected = false
            };
        }
    }
}