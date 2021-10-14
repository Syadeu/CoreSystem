using Syadeu.Collections;
using Syadeu.Collections.Proxy;
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
        //private Transform m_CanvasTransform;
        private GraphicRaycaster m_CanvasRaycaster;

        private UnsafeMultiHashMap<Entity<IEntity>, Entity<UIObjectEntity>> m_AttachedUIHashMap;

        private SceneSystem m_SceneSystem;
        private RenderSystem m_RenderSystem;
        private CoroutineSystem m_CoroutineSystem;

        public Canvas Canvas
        {
            get
            {
                if (m_Canvas == null)
                {
                    GameObject obj = m_SceneSystem.CreateGameObject("World Canvas");
                    m_Canvas = obj.AddComponent<Canvas>();
                    m_Canvas.renderMode = RenderMode.WorldSpace;
                    m_Canvas.worldCamera = m_RenderSystem.Camera;
                    obj.AddComponent<CanvasScaler>();

                    m_CanvasRaycaster = Canvas.gameObject.AddComponent<GraphicRaycaster>();
                    m_CanvasRaycaster.blockingMask = LayerMask.GetMask("UI");
                }

                return m_Canvas;
            }
        }
        public GraphicRaycaster CanvasRaycaster
        {
            get
            {
                if (m_CanvasRaycaster == null)
                {
                    m_CanvasRaycaster = Canvas.gameObject.AddComponent<GraphicRaycaster>();
                    m_CanvasRaycaster.blockingMask = LayerMask.GetMask("UI");
                }

                return m_CanvasRaycaster;
            }
        }

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
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

            if (m_Canvas != null)
            {
                Destroy(m_Canvas.gameObject);
            }

            m_RenderSystem = null;
            m_CoroutineSystem = null;
        }

        #region Binds

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;

            //GameObject obj = new GameObject("World Canvas");
            //m_CanvasTransform = obj.transform;
            //DontDestroyOnLoad(obj);
            //m_Canvas = obj.AddComponent<Canvas>();
            //m_Canvas.renderMode = RenderMode.WorldSpace;
            //obj.AddComponent<CanvasScaler>();
            //m_CanvasRaycaster = obj.AddComponent<GraphicRaycaster>();
            //m_CanvasRaycaster.blockingMask = LayerMask.GetMask("UI");
        }

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            m_RenderSystem.OnCameraChanged += M_RenderSystem_OnCameraChanged;
        }
        private void M_RenderSystem_OnCameraChanged(Camera arg1, Camera arg2)
        {
            if (m_Canvas != null)
            {
                m_Canvas.worldCamera = arg2;
            }
        }

        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }

        #endregion

        #endregion

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

        internal void InternalSetProxy(EntityBase entityBase, Entity<UIObjectEntity> entity, 
            CanvasGroup cg)
        {
            UIObjectEntity uiObject = (UIObjectEntity)entityBase;
            var ui = entity.GetComponent<UIObjectCanvasGroupComponent>();

            cg.blocksRaycasts = ui.Enabled;

            if (!uiObject.m_EnableAutoFade) return;
        }

        public void SetActive(Entity<UIObjectEntity> entity, bool enable)
        {
            if (!entity.HasComponent<UIObjectCanvasGroupComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"UI Entity({entity.RawName}) dosen\'t have any {nameof(UIObjectCanvasGroupComponent)}.");

                return;
            }

            ref var ui = ref entity.GetComponent<UIObjectCanvasGroupComponent>();
            ui.m_Enabled = enable;


        }

        #region Actor Overlay UI

        public void RegisterActorOverlayUI(Entity<ActorEntity> entity, Reference<ActorOverlayUIEntry> uiEntry)
        {
            ref var ui = ref entity.GetComponent<ActorOverlayUIComponent>();
            if (ui.m_OpenedUI.Contains(uiEntry))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.RawName}) already have {uiEntry.GetObject().Name} overlay ui.");
                return;
            }

            ui.m_OpenedUI.Add(uiEntry);

            ActorOverlayUIEntry setting = uiEntry.GetObject();

#if UNITY_EDITOR
            if (setting.m_Prefab.IsEmpty() || !setting.m_Prefab.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.RawName}) has an invalid ui entry({setting.Name}) has invalid prefab entity.");
                return;
            }
            else if (!setting.m_Prefab.GetObject().HasAttribute<ActorOverlayUIAttributeBase>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Attached UI entity({setting.m_Prefab.GetObject().Name}) at entity({entity.Name}) has no {nameof(ActorOverlayUIAttributeBase)}.");
                return;
            }
#endif

            ActorOverlayUpdateJob updateJob = new ActorOverlayUpdateJob(entity, uiEntry);
            m_AttachedUIHashMap.Add(entity.Cast<ActorEntity, IEntity>(), updateJob.UIInstance);

            if (setting.m_UpdateType != UpdateType.Manual)
            {
                m_CoroutineSystem.PostCoroutineJob(updateJob);
            }
        }
        public void UnregisterActorOverlayUI(Entity<ActorEntity> entity, Reference<ActorOverlayUIEntry> uiEntry)
        {
            ref var ui = ref entity.GetComponent<ActorOverlayUIComponent>();
            if (!ui.m_OpenedUI.Contains(uiEntry))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.RawName}) does not have {uiEntry.GetObject().Name} overlay ui.");
                return;
            }
            ui.m_OpenedUI.Remove(uiEntry);

            Entity<IEntity> targetEntity = entity.Cast<ActorEntity, IEntity>();
            if (!m_AttachedUIHashMap.TryGetFirstValue(targetEntity,
                    out Entity<UIObjectEntity> uiEntity, out var iterator))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Unexpected error");
                return;
            }

            Hash targetUI = uiEntry.GetObject().m_Prefab.Hash;
            bool found = false;

            do
            {
                if (uiEntity.Hash.Equals(targetUI))
                {
                    found = true;
                    break;
                }

            } while (m_AttachedUIHashMap.TryGetNextValue(out uiEntity, ref iterator));

            if (!found)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Unexpected error");
                return;
            }

            m_AttachedUIHashMap.Remove(targetEntity, uiEntity);
            uiEntity.Destroy();
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

        private struct ActorOverlayUpdateJob : ICoroutineJob
        {
            private Entity<ActorEntity> m_Entity;
            private Reference<ActorOverlayUIEntry> m_UI;

            private Entity<UIObjectEntity> m_InstanceObject;

            UpdateLoop ICoroutineJob.Loop => UpdateLoop.AfterTransform;
            public Entity<UIObjectEntity> UIInstance => m_InstanceObject;

            public ActorOverlayUpdateJob(Entity<ActorEntity> entity, Reference<ActorOverlayUIEntry> ui)
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

                while (m_InstanceObject.IsValid())
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

        #endregion
    }
}
