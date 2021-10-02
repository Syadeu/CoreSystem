using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorOverlayUIAttributeBase"/> 와 페어로 작동합니다.
    /// </summary>
    public class ActorOverlayUIProvider : ActorProviderBase
    {
        [Flags]
        public enum UpdateType
        {
            Manual                  =   0,
            
            Lerp                    =   0b0001,
            Instant                 =   0b0010,

            /// <summary>
            /// 카메라와 오리엔테이션을 맞춥니다.
            /// </summary>
            SyncCameraOrientation   =   0b0100,
            /// <summary>
            /// 부모(이 엔티티의 <seealso cref="ITransform"/>)와 오리엔테이션을 맞춥니다.
            /// </summary>
            SyncParentOrientation   =   0b1000
        }
        private struct UpdateJob : ICoroutineJob
        {
            private Entity<ActorEntity> m_Entity;
            private Entity<UIObjectEntity> m_UI;
            private Instance<ActorOverlayUIProvider> m_Provider;

            private UpdateType m_UpdateType;
            private float m_UpdateSpeed;

            UpdateLoop ICoroutineJob.Loop => UpdateLoop.AfterTransform;

            public UpdateJob(Entity<ActorEntity> entity, Entity<UIObjectEntity> ui, 
                Instance<ActorOverlayUIProvider> provider,
                UpdateType updateType, float updateSpeed)
            {
                m_Entity = entity;
                m_UI = ui;
                m_Provider = provider;

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

                ActorOverlayUIProvider provider = m_Provider.Object;
                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                Transform camTr;

                WaitUntil waitUntil = new WaitUntil(() => renderSystem.Camera != null);

                while (true)
                {
                    if (renderSystem.Camera == null)
                    {
                        yield return waitUntil;
                    }
                    camTr = renderSystem.Camera.transform;

                    if ((m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                    {
                        uiTr.position = entityTr.position + provider.m_Offset;
                    }
                    else if ((m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                    {
                        uiTr.position
                            = math.lerp(uiTr.position, entityTr.position + provider.m_Offset, Time.deltaTime * m_UpdateSpeed);
                    }

                    Quaternion offset = Quaternion.Euler(provider.m_OrientationOffset);
                    if ((m_UpdateType & UpdateType.SyncCameraOrientation) == UpdateType.SyncCameraOrientation)
                    {
                        Quaternion orientation = Quaternion.LookRotation(camTr.forward, Vector3.up);

                        if ((m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                        {
                            uiTr.rotation = orientation * offset;
                        }
                        else if ((m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                        {
                            uiTr.rotation = Quaternion.Lerp(uiTr.rotation, orientation * offset, Time.deltaTime * m_UpdateSpeed);
                        }
                    }
                    else if ((m_UpdateType & UpdateType.SyncParentOrientation) == UpdateType.SyncParentOrientation)
                    {
                        Quaternion orientation = entityTr.rotation;

                        if ((m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                        {
                            uiTr.rotation = orientation * offset;
                        }
                        else if ((m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                        {
                            uiTr.rotation = Quaternion.Lerp(uiTr.rotation, orientation * offset, Time.deltaTime * m_UpdateSpeed);
                        }
                    }
                    else
                    {
                        if ((m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                        {
                            uiTr.rotation = offset;
                        }
                        else if ((m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                        {
                            uiTr.rotation = Quaternion.Lerp(uiTr.rotation, offset, Time.deltaTime * m_UpdateSpeed);
                        }
                    }

                    yield return null;
                }
            }
        }

        [JsonProperty(Order = -10, PropertyName = "Prefab")]
        protected Reference<UIObjectEntity> m_Prefab = Reference<UIObjectEntity>.Empty;
        [JsonProperty(Order = -9, PropertyName = "UpdateType")]
        protected UpdateType m_UpdateType = UpdateType.Instant | UpdateType.SyncCameraOrientation;
        [JsonProperty(Order = -8, PropertyName = "UpdateSpeed")]
        protected float m_UpdateSpeed = 4;

        [Space, Header("Position")]
        [JsonProperty(Order = -7, PropertyName = "Offset")]
        protected float3 m_Offset = float3.zero;

        [Space, Header("Orientation")]
        [JsonProperty(Order = -6, PropertyName = "OrientationOffset")]
        protected float3 m_OrientationOffset = float3.zero;
        
        [JsonIgnore] private Entity<UIObjectEntity> m_InstanceObject = Entity<UIObjectEntity>.Empty;

        [JsonIgnore] private UpdateJob m_UpdateJob;
        [JsonIgnore] private CoroutineJob m_UpdateCoroutine;

        //[JsonIgnore] protected override Type[] ReceiveEventOnly => new Type[]
        //    {
        //        TypeHelper.TypeOf<IActorOverlayUIEvent>.Type
        //    };
        [JsonIgnore] public Entity<UIObjectEntity> InstanceObject => m_InstanceObject;

        protected override sealed void OnCreated(Entity<ActorEntity> entity)
        {
#if UNITY_EDITOR
            if (!m_Prefab.GetObject().HasAttribute<ActorOverlayUIAttributeBase>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Attached UI entity({m_Prefab.GetObject().Name}) at entity({Parent.Name}) has no {nameof(ActorOverlayUIAttributeBase)}.");
                return;
            }
#endif
            m_InstanceObject = Instance<UIObjectEntity>.CreateInstance(m_Prefab);
            m_InstanceObject.transform.position = entity.transform.position + m_Offset;
            m_InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().UICreated(Parent.As<IEntityData, ActorEntity>());

            if (m_UpdateType != UpdateType.Manual)
            {
                m_UpdateJob = new UpdateJob(entity, m_InstanceObject, 
                    new Instance<ActorOverlayUIProvider>(this),
                    m_UpdateType, m_UpdateSpeed);
                m_UpdateCoroutine = StartCoroutine(m_UpdateJob);
            }
        }
        protected override sealed void OnDestroy(Entity<ActorEntity> entity)
        {
            if (m_InstanceObject.IsValid())
            {
                m_InstanceObject.Destroy();
            }

            if (m_UpdateCoroutine.IsValid())
            {
                m_UpdateCoroutine.Stop();
                m_UpdateJob = default;
            }
        }
        protected override sealed void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is IActorOverlayUIEvent overlayEv)
            {
                overlayEv.OnExecute(m_InstanceObject);
                ActorOverlayUIEventHandler(ev);
                if (InstanceObject.IsValid())
                {
                    InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().UIEventReceived(ev);
                }
            }
            else
            {
                ActorEventHandler(ev);
                if (InstanceObject.IsValid())
                {
                    InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().EventReceived(ev);
                }
            }
        }

        /// <summary>
        /// <see cref="IActorOverlayUIEvent"/> 만 들어옵니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        protected virtual void ActorOverlayUIEventHandler<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        { }
        protected virtual void ActorEventHandler<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        { }
    }
}
