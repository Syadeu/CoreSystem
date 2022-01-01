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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

#if CORESYSTEM_DOTWEEN
using DG.Tweening;
#endif
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.Render
{
    public sealed class WorldCanvasSystem : PresentationSystemEntity<WorldCanvasSystem>,
        INotifySystemModule<EntityOverlayUIModule>
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
        protected override void OnShutDown()
        {
            foreach (var item in m_AttachedUIHashMap)
            {
                foreach (var entity in m_AttachedUIHashMap.GetValuesForKey(item.Key))
                {
                    entity.Destroy();
                }
            }

            if (m_Canvas != null)
            {
                Destroy(m_Canvas.gameObject);
            }
        }
        protected override void OnDispose()
        {
            m_AttachedUIHashMap.Dispose();

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

            cg.blocksRaycasts = ui.m_Enabled;

#if CORESYSTEM_DOTWEEN
            cg.DOKill();
#endif
            cg.alpha = ui.Alpha;

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

        private readonly List<Entity<UIObjectEntity>> m_AllActorOverlayUI = new List<Entity<UIObjectEntity>>();

        public void SetAlphaActorOverlayUI(float alpha)
        {
            for (int i = 0; i < m_AllActorOverlayUI.Count; i++)
            {
                Entity<UIObjectEntity> uiEntity = m_AllActorOverlayUI[i];
                uiEntity.GetComponent<UIObjectCanvasGroupComponent>().Alpha = alpha;
            }
        }
        public void SetAlphaActorOverlayUI(Entity<ActorEntity> entity, float alpha)
        {
            Entity<IEntity> targetEntity = entity.ToEntity<IEntity>();
            if (!m_AttachedUIHashMap.TryGetFirstValue(targetEntity,
                    out Entity<UIObjectEntity> uiEntity, out var iterator))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Unexpected error");
                return;
            }

            do
            {
                uiEntity.GetComponent<UIObjectCanvasGroupComponent>().Alpha = alpha;
            } while (m_AttachedUIHashMap.TryGetNextValue(out uiEntity, ref iterator));
        }
        public void SetAlphaActorOverlayUI(Entity<ActorEntity> entity, Reference<ActorOverlayUIEntry> uiEntry, float alpha)
        {
            Entity<IEntity> targetEntity = entity.ToEntity<IEntity>();
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

            uiEntity.GetComponent<UIObjectCanvasGroupComponent>().Alpha = alpha;
        }

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
            m_AttachedUIHashMap.Add(entity.ToEntity<IEntity>(), updateJob.UIInstance);

            m_AllActorOverlayUI.Add(updateJob.UIInstance);

            if (setting.m_UpdateType != Actor.UpdateType.Manual)
            {
                m_CoroutineSystem.StartCoroutine(updateJob);
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

            Entity<IEntity> targetEntity = entity.ToEntity<IEntity>();
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

            m_AllActorOverlayUI.Remove(uiEntity);
        }

        public void PostActorOverlayUIEvent(Entity<ActorEntity> entity, IActorEvent ev)
        {
            if (m_AttachedUIHashMap.TryGetFirstValue(entity.ToEntity<IEntity>(), 
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

            private Instance<ActorOverlayUIEntry> m_UISettingInstance;
            private Entity<UIObjectEntity> m_InstanceObject;

            private bool m_UseBone;
            private HumanBodyBones m_BoneTarget;

            UpdateLoop ICoroutineJob.Loop => UpdateLoop.AfterTransform;
            public Entity<UIObjectEntity> UIInstance => m_InstanceObject;

            public ActorOverlayUpdateJob(Entity<ActorEntity> entity, Reference<ActorOverlayUIEntry> ui)
            {
                m_Entity = entity;
                m_UI = ui;

                m_UISettingInstance = ui.CreateInstance();
                ActorOverlayUIEntry setting = m_UI.GetObject();

                m_UseBone = setting.m_PositionOffset.m_UseBone;
                m_BoneTarget = setting.m_PositionOffset.m_BoneTarget;

                if (m_UseBone && !m_Entity.HasAttribute<AnimatorAttribute>())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Entity({entity.RawName}) use bone but doesn\'t have {nameof(AnimatorAttribute)}.");
                    m_UseBone = false;
                }

                m_InstanceObject = setting.m_Prefab.CreateInstance();
                SetPosition(in setting);
                m_InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().UICreated(entity);
            }
            public void Dispose()
            {
                m_UISettingInstance.Destroy();
            }

            private void SetPosition(in ActorOverlayUIEntry setting)
            {
                float3 targetPosition;

                if (!m_UseBone || !m_Entity.hasProxy)
                {
                    targetPosition = m_Entity.transform.position;
                }
                else
                {
                    var anim = m_Entity.GetAttribute<AnimatorAttribute>();
                    Transform tr = anim.AnimatorComponent.Animator.GetBoneTransform(m_BoneTarget);
                    if (tr == null)
                    {
                        $"something went wrong.. missing bone {m_BoneTarget}".ToLogError();
                        targetPosition = m_Entity.transform.position;
                    }
                    else 
                        targetPosition = tr.position;
                }

                if ((setting.m_UpdateType & Actor.UpdateType.Instant) == Actor.UpdateType.Instant)
                {
                    var tr = m_InstanceObject.transform;
                    tr.position = targetPosition + setting.m_PositionOffset.m_Offset;
                }
                else if ((setting.m_UpdateType & Actor.UpdateType.Lerp) == Actor.UpdateType.Lerp)
                {
                    var uiTr = m_InstanceObject.transform;

                    uiTr.position
                        = math.lerp(uiTr.position, targetPosition + setting.m_PositionOffset.m_Offset, Time.deltaTime * setting.m_UpdateSpeed);
                }
            }

            public IEnumerator Execute()
            {
                ITransform
                    entityTr = m_Entity.transform,
                    uiTr = m_InstanceObject.transform;

                ActorOverlayUIEntry setting = m_UISettingInstance.GetObject();
                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                Transform camTr;

                WaitUntil waitUntil = new WaitUntil(() => renderSystem.Camera != null);

                while (true)
                {
                    if (renderSystem.Camera == null)
                    {
                        yield return waitUntil;
                    }
                    if (!m_Entity.IsValid())
                    {
                        break;
                    }

                    camTr = renderSystem.Camera.transform;

                    SetPosition(in setting);
                    //if ((setting.m_UpdateType & Actor.UpdateType.Instant) == Actor.UpdateType.Instant)
                    //{
                    //    uiTr.position = entityTr.position + setting.m_PositionOffset.m_Offset;
                    //}
                    //else if ((setting.m_UpdateType & Actor.UpdateType.Lerp) == Actor.UpdateType.Lerp)
                    //{
                    //    uiTr.position
                    //        = math.lerp(uiTr.position, entityTr.position + setting.m_Offset, Time.deltaTime * setting.m_UpdateSpeed);
                    //}

                    Quaternion offset = Quaternion.Euler(setting.m_OrientationOffset);
                    if ((setting.m_UpdateType & Actor.UpdateType.SyncCameraOrientation) == Actor.UpdateType.SyncCameraOrientation)
                    {
                        Quaternion orientation = Quaternion.LookRotation(camTr.forward, Vector3.up);

                        if ((setting.m_UpdateType & Actor.UpdateType.Instant) == Actor.UpdateType.Instant)
                        {
                            uiTr.rotation = orientation * offset;
                        }
                        else if ((setting.m_UpdateType & Actor.UpdateType.Lerp) == Actor.UpdateType.Lerp)
                        {
                            uiTr.rotation = Quaternion.Lerp(uiTr.rotation, orientation * offset, Time.deltaTime * setting.m_UpdateSpeed);
                        }
                    }
                    else if ((setting.m_UpdateType & Actor.UpdateType.SyncParentOrientation) == Actor.UpdateType.SyncParentOrientation)
                    {
                        Quaternion orientation = entityTr.rotation;

                        if ((setting.m_UpdateType & Actor.UpdateType.Instant) == Actor.UpdateType.Instant)
                        {
                            uiTr.rotation = orientation * offset;
                        }
                        else if ((setting.m_UpdateType & Actor.UpdateType.Lerp) == Actor.UpdateType.Lerp)
                        {
                            uiTr.rotation = Quaternion.Lerp(uiTr.rotation, orientation * offset, Time.deltaTime * setting.m_UpdateSpeed);
                        }
                    }
                    else
                    {
                        if ((setting.m_UpdateType & Actor.UpdateType.Instant) == Actor.UpdateType.Instant)
                        {
                            uiTr.rotation = offset;
                        }
                        else if ((setting.m_UpdateType & Actor.UpdateType.Lerp) == Actor.UpdateType.Lerp)
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
