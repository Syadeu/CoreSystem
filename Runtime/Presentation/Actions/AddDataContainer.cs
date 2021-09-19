using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Add DataContainer")]
    [ReflectionDescription("" +
        "이 Entity 를 해당 키 값으로 등록합니다. " +
        "Type 은 EntityData<IEntityData> 로 등록됩니다.")]
    public sealed class AddDataContainer : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Key")]
        private string m_Key = string.Empty;

        [JsonIgnore] private DataContainerSystem m_DataContainer;
        [JsonIgnore] private Hash m_KeyHash = Hash.Empty;

        protected override void OnCreated()
        {
            m_DataContainer = PresentationSystem<DataContainerSystem>.System;
            if (!string.IsNullOrEmpty(m_Key))
            {
                m_KeyHash = DataContainerSystem.ToDataHash(m_Key);
            }
        }
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (m_KeyHash.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(AddDataContainer)}({Name}) error. Key cannot be a null or empty.");
                return;
            }

            m_DataContainer.Enqueue(m_KeyHash, entity);
        }
    }
}
