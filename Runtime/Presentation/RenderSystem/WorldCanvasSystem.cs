using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.Render
{
    public sealed class WorldCanvasSystem : PresentationSystemEntity<WorldCanvasSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private Canvas m_Canvas;
        private Transform m_CanvasTransform;
        private GraphicRaycaster m_CanvasRaycaster;

        private UnsafeMultiHashMap<Entity<IEntity>, Entity<UIObjectEntity>> m_AttachedUIHashMap;

        private RenderSystem m_RenderSystem;
        private CoroutineSystem m_CoroutineSystem;

        public Canvas Canvas => m_Canvas;

        protected override PresentationResult OnInitialize()
        {
            GameObject obj = new GameObject("World Canvas");
            m_CanvasTransform = obj.transform;
            DontDestroyOnLoad(obj);
            m_Canvas = obj.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.WorldSpace;
            obj.AddComponent<CanvasScaler>();
            m_CanvasRaycaster = obj.AddComponent<GraphicRaycaster>();
            m_CanvasRaycaster.blockingMask = LayerMask.GetMask("UI");

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);

            m_AttachedUIHashMap = new UnsafeMultiHashMap<Entity<IEntity>, Entity<UIObjectEntity>>(1024, AllocatorManager.Persistent);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            foreach (var item in m_AttachedUIHashMap)
            {
                foreach (var entity in m_AttachedUIHashMap.GetValuesForKey(item.Key))
                {
                    entity.Destroy();
                }
            }

            m_AttachedUIHashMap.Dispose();

            Destroy(m_Canvas.gameObject);

            m_RenderSystem = null;
            m_CoroutineSystem = null;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            m_RenderSystem.OnCameraChanged += M_RenderSystem_OnCameraChanged;
        }
        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }

        private void M_RenderSystem_OnCameraChanged(Camera arg1, Camera arg2)
        {
            m_Canvas.worldCamera = arg2;
        }

        public void RemoveAllOverlayUI(Entity<IEntity> entity)
        {
            if (m_AttachedUIHashMap.TryGetFirstValue(entity, out Entity<UIObjectEntity> uiEntity, out var iterator))
            {
                do
                {
                    uiEntity.Destroy();

                } while (m_AttachedUIHashMap.TryGetNextValue(out uiEntity, ref iterator));
            }

            m_AttachedUIHashMap.Remove(entity);
        }
        public void RegisterActorOverlayUI(Entity<ActorEntity> entity, Reference<ActorOverlayUIEntry> uiEntry)
        {
            ActorOverlayUIEntry setting = uiEntry.GetObject();

#if UNITY_EDITOR
            if (!setting.m_Prefab.GetObject().HasAttribute<ActorOverlayUIAttributeBase>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Attached UI entity({setting.m_Prefab.GetObject().Name}) at entity({entity.Name}) has no {nameof(ActorOverlayUIAttributeBase)}.");
                return;
            }
#endif

            UpdateJob updateJob = new UpdateJob(entity, uiEntry);
            m_AttachedUIHashMap.Add(entity.Cast<ActorEntity, IEntity>(), updateJob.UIInstance);

            if (setting.m_UpdateType != UpdateType.Manual)
            {
                m_CoroutineSystem.PostCoroutineJob(updateJob);
            }
        }
        public void PostActorOverlayUIEvent<TEvent>(Entity<ActorEntity> entity, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            if (m_AttachedUIHashMap.TryGetFirstValue(entity.Cast<ActorEntity, IEntity>(), 
                    out Entity<UIObjectEntity> uiEntity, out var iterator))
            {
                do
                {
                    uiEntity.GetAttribute<ActorOverlayUIAttributeBase>().EventReceived(ev);

                } while (m_AttachedUIHashMap.TryGetNextValue(out uiEntity, ref iterator));
            }
        }

        private struct UpdateJob : ICoroutineJob
        {
            private Entity<ActorEntity> m_Entity;
            private Reference<ActorOverlayUIEntry> m_UI;

            private Entity<UIObjectEntity> m_InstanceObject;

            UpdateLoop ICoroutineJob.Loop => UpdateLoop.AfterTransform;
            public Entity<UIObjectEntity> UIInstance => m_InstanceObject;

            public UpdateJob(Entity<ActorEntity> entity, Reference<ActorOverlayUIEntry> ui)
            {
                m_Entity = entity;
                m_UI = ui;

                ActorOverlayUIEntry setting = m_UI.GetObject();
                m_InstanceObject = Instance<UIObjectEntity>.CreateInstance(setting.m_Prefab);
                m_InstanceObject.transform.position = entity.transform.position + setting.m_Offset;
                m_InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().UICreated(entity);
            }
            public void Dispose()
            {
            }
            public IEnumerator Execute()
            {
                ITransform
                    entityTr = m_Entity.transform,
                    uiTr = m_InstanceObject.transform;

                ActorOverlayUIEntry setting = m_UI.GetObject();
                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                Transform camTr;

                WaitUntil waitUntil = new WaitUntil(() => renderSystem.Camera != null);

                while (m_UI.IsValid())
                {
                    if (renderSystem.Camera == null)
                    {
                        yield return waitUntil;
                    }
                    camTr = renderSystem.Camera.transform;

                    if ((setting.m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                    {
                        uiTr.position = entityTr.position + setting.m_Offset;
                    }
                    else if ((setting.m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                    {
                        uiTr.position
                            = math.lerp(uiTr.position, entityTr.position + setting.m_Offset, Time.deltaTime * setting.m_UpdateSpeed);
                    }

                    Quaternion offset = Quaternion.Euler(setting.m_OrientationOffset);
                    if ((setting.m_UpdateType & UpdateType.SyncCameraOrientation) == UpdateType.SyncCameraOrientation)
                    {
                        Quaternion orientation = Quaternion.LookRotation(camTr.forward, Vector3.up);

                        if ((setting.m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                        {
                            uiTr.rotation = orientation * offset;
                        }
                        else if ((setting.m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                        {
                            uiTr.rotation = Quaternion.Lerp(uiTr.rotation, orientation * offset, Time.deltaTime * setting.m_UpdateSpeed);
                        }
                    }
                    else if ((setting.m_UpdateType & UpdateType.SyncParentOrientation) == UpdateType.SyncParentOrientation)
                    {
                        Quaternion orientation = entityTr.rotation;

                        if ((setting.m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                        {
                            uiTr.rotation = orientation * offset;
                        }
                        else if ((setting.m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                        {
                            uiTr.rotation = Quaternion.Lerp(uiTr.rotation, orientation * offset, Time.deltaTime * setting.m_UpdateSpeed);
                        }
                    }
                    else
                    {
                        if ((setting.m_UpdateType & UpdateType.Instant) == UpdateType.Instant)
                        {
                            uiTr.rotation = offset;
                        }
                        else if ((setting.m_UpdateType & UpdateType.Lerp) == UpdateType.Lerp)
                        {
                            uiTr.rotation = Quaternion.Lerp(uiTr.rotation, offset, Time.deltaTime * setting.m_UpdateSpeed);
                        }
                    }

                    yield return null;
                }
            }
        }
    }
}
