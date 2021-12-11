// Copyright 2021 Seung Ha Kim
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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Mono;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections;
using System.ComponentModel;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

namespace Syadeu.Presentation.Attributes
{
    [DisplayName("Attribute: NavAgent")]
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class NavAgentAttribute : AttributeBase,
        INotifyComponent<NavAgentComponent>
    {
        [JsonProperty(Order = 0, PropertyName = "AgentType")] public int m_AgentType = 0;
        [JsonProperty(Order = 1, PropertyName = "BaseOffset")] public float m_BaseOffset = 0;

        [Space, Header("Steering")]
        [JsonProperty(Order = 2, PropertyName = "Speed")] public float m_Speed = 3.5f;
        [JsonProperty(Order = 3, PropertyName = "AngularSpeed")] public float m_AngularSpeed = 120;
        [JsonProperty(Order = 4, PropertyName = "Acceleration")] public float m_Acceleration = 8;
        [JsonProperty(Order = 5, PropertyName = "StoppingDistance")] public float m_StoppingDistance = 0;

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 6, PropertyName = "OnMoveActions")]
        internal Reference<TriggerAction>[] m_OnMoveActions = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 7, PropertyName = "UpdateTRSWhile")]
        internal Reference<TriggerPredicateAction>[] m_UpdateTRSWhile = Array.Empty<Reference<TriggerPredicateAction>>();

        [JsonIgnore] public NavMeshAgent NavMeshAgent { get; internal set; }
    }

    internal sealed class NavAgentProcessor : AttributeProcessor<NavAgentAttribute>, 
        IAttributeOnProxy
    {
        protected override void OnCreated(NavAgentAttribute attribute, EntityData<IEntityData> entity)
        {
            ref var com = ref entity.GetComponent<NavAgentComponent>();
            com.m_OnMoveActions = attribute.m_OnMoveActions.ToFixedList64();
            com.m_UpdateTRSWhile = attribute.m_UpdateTRSWhile.ToFixedList64();
        }
        public void OnProxyCreated(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            NavAgentAttribute att = (NavAgentAttribute)attribute;

            att.NavMeshAgent = monoObj.GetComponent<NavMeshAgent>();
            if (att.NavMeshAgent == null) att.NavMeshAgent = monoObj.AddComponent<NavMeshAgent>();

            UpdateNavMeshAgent(att, att.NavMeshAgent);

            att.NavMeshAgent.enabled = true;
        }
        public void OnProxyRemoved(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            NavAgentAttribute att = (NavAgentAttribute)attribute;

            att.NavMeshAgent = null;
        }

        private static void UpdateNavMeshAgent(NavAgentAttribute att, NavMeshAgent agent)
        {
            agent.agentTypeID = att.m_AgentType;
            agent.baseOffset = att.m_BaseOffset;

            agent.speed = att.m_Speed;
            agent.angularSpeed = att.m_AngularSpeed;
            agent.acceleration = att.m_Acceleration;
            agent.stoppingDistance = att.m_StoppingDistance;

            //agent.updatePosition = false;
            //agent.updateRotation = false;
        }
    }
}
