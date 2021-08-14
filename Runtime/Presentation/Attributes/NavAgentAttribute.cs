using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Map;
using System;
using System.Collections;
//using Syadeu.ThreadSafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class NavAgentAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "AgentType")] public int m_AgentType = 0;
        [JsonProperty(Order = 1, PropertyName = "BaseOffset")] public float m_BaseOffset = 0;

        [Space, Header("Steering")]
        [JsonProperty(Order = 2, PropertyName = "Speed")] public float m_Speed = 3.5f;
        [JsonProperty(Order = 3, PropertyName = "AngularSpeed")] public float m_AngularSpeed = 120;
        [JsonProperty(Order = 4, PropertyName = "Acceleration")] public float m_Acceleration = 8;
        [JsonProperty(Order = 5, PropertyName = "StoppingDistance")] public float m_StoppingDistance = 0;

        [JsonIgnore] public NavMeshAgent NavMeshAgent { get; internal set; }
        [JsonIgnore] public bool IsMoving { get; internal set; }
        [JsonIgnore] public Vector3 Direction => NavMeshAgent == null ? Vector3.zero : NavMeshAgent.desiredVelocity;
        [JsonIgnore] private CoreRoutine Routine { get; set; }
        [JsonIgnore] public Vector3 PreviousTarget { get; set; }

        public void MoveTo(Vector3 point)
        {
            if (!NavMeshAgent.isOnNavMesh)
            {
                NavMeshAgent.enabled = false;
                NavMeshAgent.enabled = true;
            }

            PresentationSystem<EventSystem>.System
                .PostEvent(OnMoveStateChangedEvent.GetEvent(Parent, OnMoveStateChangedEvent.MoveState.AboutToMove));

            NavMeshAgent.ResetPath();
            NavMeshAgent.SetDestination(point);
            PreviousTarget = point;
            IsMoving = true;

            if (Routine.IsValid() && Routine.IsRunning)
            {
                CoreSystem.RemoveUnityUpdate(Routine);
            }
            Routine = CoreSystem.StartUnityUpdate(this, Updater());
        }
        private IEnumerator Updater()
        {
            EventSystem eventSystem = PresentationSystem<EventSystem>.System;
            Entity<IEntity> parent = Parent;
            ProxyTransform tr = parent.transform;

            if (!tr.hasProxy) yield break;

            while (NavMeshAgent.pathPending ||
                NavMeshAgent.desiredVelocity.magnitude == 0)
            {
                if (!tr.hasProxy) yield break;

                yield return null;
            }

            eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(parent, OnMoveStateChangedEvent.MoveState.OnMoving));

            while (
                tr.hasProxy &&
                NavMeshAgent.desiredVelocity.magnitude > 0 &&
                NavMeshAgent.remainingDistance > .2f)
            {
                if (!tr.hasProxy) yield break;

                tr.Synchronize(ProxyTransform.SynchronizeOption.TR);
                eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(parent, OnMoveStateChangedEvent.MoveState.OnMoving));
                
                yield return null;
            }

            tr.position = PreviousTarget;
            if (NavMeshAgent.isOnNavMesh) NavMeshAgent.ResetPath();

            IsMoving = false;

            eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(parent, OnMoveStateChangedEvent.MoveState.Stopped | OnMoveStateChangedEvent.MoveState.Idle));
        }
    }

    internal sealed class NavAgentProcessor : AttributeProcessor<NavAgentAttribute>, 
        IAttributeOnProxyCreatedSync, IAttributeOnProxyRemovedSync
    {
        public void OnProxyCreatedSync(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            NavAgentAttribute att = (NavAgentAttribute)attribute;

            att.NavMeshAgent = monoObj.GetComponent<NavMeshAgent>();
            if (att.NavMeshAgent == null) att.NavMeshAgent = monoObj.AddComponent<NavMeshAgent>();

            UpdateNavMeshAgent(att, att.NavMeshAgent);

            att.NavMeshAgent.enabled = true;
        }
        public void OnProxyRemovedSync(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            NavAgentAttribute att = (NavAgentAttribute)attribute;

            if (att.IsMoving)
            {
                ProxyTransform tr = entity.transform;

                NavMeshAgent agent = monoObj.GetComponent<NavMeshAgent>();
                if (agent.isOnNavMesh) agent.ResetPath();
                agent.enabled = false;

                tr.position = att.PreviousTarget;
                att.IsMoving = false;

                EventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(entity, OnMoveStateChangedEvent.MoveState.Teleported | OnMoveStateChangedEvent.MoveState.Idle));
            }

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
        }
    }

}
