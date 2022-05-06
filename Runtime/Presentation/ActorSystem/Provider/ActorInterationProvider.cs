// Copyright 2022 Seung Ha Kim
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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System.Linq;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorEntity"/> 가 <see cref="InteractableComponent"/> 를 가진 
    /// 다른 오브젝트와 상호작용을 할 수 있게하는 Provider 입니다.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public sealed class ActorInterationProvider : ActorProviderBase<ActorInteractionComponent>,
        INotifyComponent<InteractableComponent>
    {
        // target == parent
        [SerializeField, JsonProperty(Order = 0, PropertyName = "OnInteractionConstAction")]
        public ConstActionReferenceArray m_OnInteractionConstAction = ConstActionReferenceArray.Empty;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "OnInteractionTriggerAction")]
        public ArrayWrapper<Reference<TriggerAction>> m_OnInteractionTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;

        [Space]
        [SerializeField, JsonProperty(Order = 2, PropertyName = "Interaction")]
        internal Reference<InteractionReferenceData> m_Interaction = Reference<InteractionReferenceData>.Empty;

        protected override void OnInitialize(in Entity<IEntityData> parent, ref ActorInteractionComponent component)
        {
            component = new ActorInteractionComponent(
                m_OnInteractionConstAction, m_OnInteractionTriggerAction);
        }
    }
    internal sealed class ActorInterationProviderProcessor : EntityProcessor<ActorInterationProvider>
    {
        protected override void OnCreated(ActorInterationProvider obj)
        {
            ref var interactable = ref obj.GetComponent<InteractableComponent>();
            interactable = new InteractableComponent(0);

            if (obj.m_Interaction.IsEmpty() || !obj.m_Interaction.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"interaction is null");
            }
            else
            {
                interactable.Setup(obj.m_Interaction);
            }
        }
    }

    /// <summary>
    /// <see cref="ActorInterationProvider"/> 에서 사용되는 컴포넌트입니다.
    /// </summary>
    public struct ActorInteractionComponent : IActorProviderComponent
    {
        private Fixed8<FixedConstAction> m_OnInteractionConstAction;
        private FixedReferenceList16<TriggerAction> m_OnInteractionTriggerAction;

        public ActorInteractionComponent(
            ConstActionReferenceArray onInteractionConstAction,
            ArrayWrapper<Reference<TriggerAction>> onInteractionTriggerAction)
        {
            m_OnInteractionConstAction 
                = new Fixed8<FixedConstAction>(onInteractionConstAction.Select(t => new FixedConstAction(t)));
            m_OnInteractionTriggerAction = onInteractionTriggerAction.ToFixedList16();
        }

        public void ExecuteOnInteraction(InstanceID actor)
        {
            m_OnInteractionConstAction.Execute(actor);
            m_OnInteractionTriggerAction.Execute(actor);
        }
    }
}
