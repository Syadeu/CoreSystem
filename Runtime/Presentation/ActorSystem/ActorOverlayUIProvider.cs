using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public class ActorOverlayUIProvider : ActorProviderBase
    {
        public enum UpdateType
        {
            Manual,
            
            Lerp,
            Instant
        }
        private struct UpdateJob : ICoroutineJob
        {
            private Entity<ActorEntity> m_Entity;
            private Entity<UIObjectEntity> m_UI;
            private float3 m_Offset;
            private UpdateType m_UpdateType;
            private float m_UpdateSpeed;

            public UpdateJob(Entity<ActorEntity> entity, Entity<UIObjectEntity> ui, float3 offset,
                UpdateType updateType, float updateSpeed)
            {
                m_Entity = entity;
                m_UI = ui;
                m_Offset = offset;
                m_UpdateType = updateType;
                m_UpdateSpeed = updateSpeed;
            }
            public void Dispose()
            {
            }
            public IEnumerator Execute()
            {
                ITransform 
                    entityTr = m_Entity.transform,
                    uiTr = m_UI.transform;

                while (true)
                {
                    if ((m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                    {
                        uiTr.position = entityTr.position + m_Offset;
                    }
                    else if ((m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                    {
                        uiTr.position
                            = math.lerp(uiTr.position, entityTr.position + m_Offset, Time.deltaTime * m_UpdateSpeed);
                    }

                    yield return null;
                }
            }
        }

        [JsonProperty(Order = 0, PropertyName = "Prefab")]
        protected Reference<UIObjectEntity> m_Prefab = Reference<UIObjectEntity>.Empty;
        [JsonProperty(Order = 1, PropertyName = "Offset")]
        protected float3 m_Offset = float3.zero;
        [JsonProperty(Order = 2, PropertyName = "UpdateType")]
        protected UpdateType m_UpdateType = UpdateType.Instant;
        [JsonProperty(Order = 3, PropertyName = "UpdateSpeed")]
        protected float m_UpdateSpeed = 4;

        [JsonIgnore] private Entity<UIObjectEntity> m_InstanceObject = Entity<UIObjectEntity>.Empty;

        [JsonIgnore] private UpdateJob m_UpdateJob;
        [JsonIgnore] private CoroutineJob m_UpdateCoroutine;

        [JsonIgnore] protected override Type[] ReceiveEventOnly => new Type[]
            {
                TypeHelper.TypeOf<IActorOverlayUIEvent>.Type
            };
        [JsonIgnore] public Entity<UIObjectEntity> InstanceObject => m_InstanceObject;

        protected override sealed void OnCreated(Entity<ActorEntity> entity)
        {
            if (!m_Prefab.GetObject().HasAttribute<ActorOverlayUIAttributeBase>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Attached UI entity({m_Prefab.GetObject().Name}) at entity({Parent.Name}) has no {nameof(ActorOverlayUIAttributeBase)}.");
                return;
            }

            m_InstanceObject = Instance<UIObjectEntity>.CreateInstance(m_Prefab);
            m_InstanceObject.transform.position = entity.transform.position + m_Offset;
            m_InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().UICreated(Parent);

            if (m_UpdateType != UpdateType.Manual)
            {
                m_UpdateJob = new UpdateJob(entity, m_InstanceObject, m_Offset, m_UpdateType, m_UpdateSpeed);
                m_UpdateCoroutine = StartCoroutine(m_UpdateJob);
            }
        }
        protected override sealed void OnDestroy(Entity<ActorEntity> entity)
        {
            if (m_InstanceObject.IsValid())
            {
                m_InstanceObject.Destroy();
            }

            if (m_UpdateCoroutine.IsValid() && 
                m_UpdateType != UpdateType.Manual)
            {
                m_UpdateCoroutine.Stop();
                m_UpdateJob = default;
            }
        }
        protected override sealed void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is IActorOverlayUIEvent actorAttackEvent)
            {
                ActorOverlayUIEventHandler(actorAttackEvent);
                if (InstanceObject.IsValid())
                {
                    InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().UIEventReceived(actorAttackEvent);
                }
            }
        }

        protected virtual void ActorOverlayUIEventHandler(IActorOverlayUIEvent ev)
        {

        }
    }
}
