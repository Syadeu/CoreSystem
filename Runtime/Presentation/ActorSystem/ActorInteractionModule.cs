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

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorInteractionModule : PresentationSystemModule<ActorSystem>
    {
        private InputSystem m_InputSystem;
        private EventSystem m_EventSystem;
        private EntityRaycastSystem m_EntityRaycastSystem;
        private RenderSystem m_RenderSystem;

        /// <summary>
        /// 상호작용을 행하는 주체 (ex. 사람이 물건을 집는다 에서 사람)
        /// </summary>
        private static InstanceID s_InteractingControlAtThisFrame;
        /// <summary>
        /// 상호작용의 목표 (ex. 사람이 물건을 집는다 에서 물건)
        /// </summary>
        private static Entity<IEntity> s_InteractingEntityAtThisFrame;

        /// <inheritdoc cref="s_InteractingControlAtThisFrame"/>
        public static Entity<IEntity> InteractingControlAtThisFrame => s_InteractingControlAtThisFrame.GetEntity<IEntity>();
        /// <inheritdoc cref="s_InteractingEntityAtThisFrame"/>
        public static Entity<IEntity> InteractingEntityAtThisFrame => s_InteractingEntityAtThisFrame;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntityRaycastSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
        }
        protected override void OnShutDown()
        {
            var inputAction = m_InputSystem.GetUserActionKeyBinding(UserActionType.Interaction);
            inputAction.performed -= OnInteractionKeyPressed;

        }
        protected override void OnDispose()
        {
            m_InputSystem = null;
            m_EventSystem = null;
            m_EntityRaycastSystem = null;
            m_RenderSystem = null;
        }

        #region Binds

        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(EntityRaycastSystem other)
        {
            m_EntityRaycastSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        #endregion

        protected override void OnStartPresentation()
        {
            var inputAction = m_InputSystem.GetUserActionKeyBinding(UserActionType.Interaction);
            inputAction.performed += OnInteractionKeyPressed;


        }

        private void OnInteractionKeyPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            "key inn".ToLog();
            IReadOnlyList<InstanceID> currentControls = System.CurrentControls;
            for (int i = 0; i < currentControls.Count; i++)
            {
                InstanceID element = currentControls[i];
                if (!element.HasComponent<ActorInteractionComponent>())
                {
                    "doesnt have interaction com".ToLog();
                    continue;
                }
                ActorInteractionComponent component = element.GetComponent<ActorInteractionComponent>();
                IEnumerable<Entity<IEntity>> nearbyInteractables = GetInteractables(element);
                if (!nearbyInteractables.Any())
                {
                    "doesnt have nearby interaction obj".ToLog();
                }

                s_InteractingControlAtThisFrame = element;
                foreach (Entity<IEntity> interactable in nearbyInteractables)
                {
                    s_InteractingEntityAtThisFrame = interactable;

                    InteractableComponent targetInteractableCom 
                        = interactable.GetComponentReadOnly<InteractableComponent>();
                    targetInteractableCom.Execute(ItemState.InteractionKey, element);

                    $"actor({element.GetEntity().Name}) interacting {interactable.Name}".ToLog();
                    
                    ProcessOnInteraction(element, interactable);
                }

                s_InteractingEntityAtThisFrame = Entity<IEntity>.Empty;
            }
            s_InteractingControlAtThisFrame = InstanceID.Empty;
        }

        private void ProcessOnInteraction(InstanceID entity, InstanceID target)
        {
            m_EventSystem.PostEvent(ActorOnInteractionEvent.GetEvent(entity, target));

            if (target.HasComponent<ActorItemComponent>())
            {
                ActorItemAttribute att = target.GetEntity().GetAttribute<ActorItemAttribute>();
                att.GeneralInfo.ExecuteOnInteract(entity);
            }
        }

        #endregion

        /// <summary>
        /// 해당 위치의 범위내 <see cref="InteractableComponent"/> 를 가진 모든 엔티티를 반환합니다.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public IEnumerable<Entity<IEntity>> GetInteractables(float3 position, float radius)
        {
            IEnumerable<RaycastInfo> infos = m_EntityRaycastSystem
                .SphereCastAll(position, radius, float3.zero)
                .Where(t =>
                {
                    return t.entity.HasComponent<InteractableComponent>();
                });

            return infos.Select(t => t.entity);
        }
        public IEnumerable<Entity<IEntity>> GetInteractables(InstanceID entity)
        {
            var triggerBound = entity.GetEntity().GetAttribute<TriggerBoundAttribute>();
            IEnumerable<Entity<IEntity>> infos = triggerBound.Triggered
                .Where(t =>
                {
                    return t.HasComponent<InteractableComponent>();
                });

            return infos;
        }
    }

    /// <summary>
    /// <see cref="ActorEntity"/> 와 상호작용을 할 수 있는 <see cref="Entities.EntityBase"/> 가 가지는 컴포넌트입니다.
    /// </summary>
    [BurstCompatible]
    public struct InteractableComponent : IEntityComponent, IDisposable
    {
        [BurstCompatible]
        public struct State
        {
            [MarshalAs(UnmanagedType.U1)]
            public bool interactable;
            public FixedReferenceList64<TriggerAction> triggerAction;
            public Fixed8<FixedConstAction> constAction;

            public State(bool interactable, 
                FixedReferenceList64<TriggerAction> triggerAction, Fixed8<FixedConstAction> constAction)
            {
                this.interactable = interactable;
                this.triggerAction = triggerAction;
                this.constAction = constAction;
            }
        }
        private FixedReference<UIObjectEntity> m_UI;
        // InteractableState
        private UnsafeHashMap<int, State> m_InteractableStates;
        private ItemState m_CurrentState;

        private InstanceID<UIObjectEntity> m_CreatedUI;

        public ItemState CurrentState { get => m_CurrentState; set => m_CurrentState = value; }
        public FixedReference<UIObjectEntity> UI { get => m_UI; set => m_UI = value; }

        internal InteractableComponent(int unused)
        {
            this = default(InteractableComponent);

            m_InteractableStates = new UnsafeHashMap<int, State>(
                TypeHelper.Enum<ItemState>.Length, 
                AllocatorManager.Persistent);
        }
        public void Setup(InteractionReferenceData interaction)
        {
            m_UI = interaction.m_InteractionUI;
            m_CurrentState = ItemState.Default;

            Set(ItemState.Grounded,
                new State(
                    interaction.m_OnGrounded,
                    interaction.m_OnGroundedTriggerAction.ToFixedList64(),
                    new Fixed8<FixedConstAction>(interaction.m_OnGroundedConstAction.Select(t => new FixedConstAction(t)))
                    ));
            Set(ItemState.Equiped,
                new State(
                    interaction.m_OnEquiped,
                    interaction.m_OnEquipedTriggerAction.ToFixedList64(),
                    new Fixed8<FixedConstAction>(interaction.m_OnEquipedConstAction.Select(t => new FixedConstAction(t)))
                    ));
            Set(ItemState.Stored,
                new State(
                    interaction.m_OnStored,
                    interaction.m_OnStoredTriggerAction.ToFixedList64(),
                    new Fixed8<FixedConstAction>(interaction.m_OnStoredConstAction.Select(t => new FixedConstAction(t)))
                    ));
        }
        void IDisposable.Dispose()
        {
            m_InteractableStates.Dispose();
        }

        internal void Execute(ItemState type, InstanceID caller)
        {
            if (m_InteractableStates.TryGetValue((int)(m_CurrentState | type), out var state) &&
                state.interactable)
            {
                state.constAction.Execute(caller);
                state.triggerAction.Execute(caller);
            }
        }
        public void Set(ItemState type, State state)
        {
            m_InteractableStates[(int)type] = state;
        }
        internal void CreateUI(float3 pos, quaternion rot)
        {
            if (m_UI.IsEmpty()) return;
#if DEBUG_MODE
            else if (!m_CreatedUI.IsEmpty())
            {
                "??".ToLogError();
                return;
            }
            else if (!m_UI.IsValid())
            {
                "??".ToLogError();
                return;
            }
#endif
            m_CreatedUI = m_UI.CreateEntity(pos, rot, 1);
        }
        internal void RemoveUI()
        {
            if (m_CreatedUI.IsEmpty())
            {
                return;
            }

            m_CreatedUI.GetEntity().Destroy();
            m_CreatedUI = InstanceID<UIObjectEntity>.Empty;
        }
    }
    internal sealed class InteractableComponentProcessor : ComponentProcessor<InteractableComponent>
    {
        private RenderSystem m_RenderSystem;
        //private RenderTexture m_RT;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_RenderSystem = null;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            //m_RT = new RenderTexture(256, 256, 24);
        }

        protected override void OnCreated(in InstanceID id, ref InteractableComponent component)
        {
            component = new InteractableComponent(0);

            Entity<IEntity> entity = id.GetEntity<IEntity>();
            if (!entity.HasAttribute<TriggerBoundAttribute>())
            {
                return;
            }
            TriggerBoundAttribute att = entity.GetAttribute<TriggerBoundAttribute>();
            att.OnTriggerBoundEvent += Att_OnTriggerBoundEvent;
        }
        protected override void OnDestroy(in InstanceID id, ref InteractableComponent component)
        {
            Entity<IEntity> entity = id.GetEntity<IEntity>();
            if (!entity.HasAttribute<TriggerBoundAttribute>())
            {
                return;
            }
            TriggerBoundAttribute att = entity.GetAttribute<TriggerBoundAttribute>();
            att.OnTriggerBoundEvent -= Att_OnTriggerBoundEvent;
        }

        private void Att_OnTriggerBoundEvent(Entity<IEntity> source, Entity<IEntity> target, bool entered)
        {
            if (entered)
            {
                var tr = source.transform;
                source.GetComponent<InteractableComponent>().CreateUI(tr.position, quaternion.identity);

                return;
            }

            source.GetComponent<InteractableComponent>().RemoveUI();

            //m_RenderSystem.GetProjectionCamera()
            $"open interaction ui for {target.Name}".ToLog();
        }
    }
}
