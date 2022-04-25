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
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorInteractionModule : PresentationSystemModule<ActorSystem>
    {
        private InputSystem m_InputSystem;
        private EventSystem m_EventSystem;
        private EntityRaycastSystem m_EntityRaycastSystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntityRaycastSystem>(Bind);
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

        #endregion

        protected override void OnStartPresentation()
        {
            var inputAction = m_InputSystem.GetUserActionKeyBinding(UserActionType.Interaction);
            inputAction.performed += OnInteractionKeyPressed;
        }

        private void OnInteractionKeyPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            m_EventSystem.PostEvent(OnInteractionKeyPressedEvent.GetEvent());

            // TODO : Temp Codes

            IReadOnlyList<InstanceID> currentControls = System.CurrentControls;
            List<InstanceID> interactableEntities = new List<InstanceID>();
            for (int i = 0; i < currentControls.Count; i++)
            {
                InstanceID element = currentControls[i];
                if (!element.HasComponent<ActorInteractionComponent>())
                {
                    continue;
                }
                ActorInteractionComponent component = element.GetComponent<ActorInteractionComponent>();

                interactableEntities.Add(element);
                ProxyTransform tr = element.GetTransform();
                IEnumerable<Entity<IEntity>> nearbyInteractables = GetInteractables(tr.position, component.interactionRange);
                
            }

            

            // TODO : Temp Codes
        }

        #endregion

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
    }

    public sealed class OnInteractionKeyPressedEvent : SynchronizedEvent<OnInteractionKeyPressedEvent>
    {
        public static OnInteractionKeyPressedEvent GetEvent()
        {
            var ev = Dequeue();

            return ev;
        }

        protected override void OnTerminate()
        {
        }
    }

    /// <summary>
    /// <see cref="ActorEntity"/> 와 상호작용을 할 수 있는 <see cref="Entities.EntityBase"/> 가 가지는 컴포넌트입니다.
    /// </summary>
    [BurstCompatible]
    public struct InteractableComponent : IEntityComponent, IDisposable
    {
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
        // InteractableState
        private UnsafeHashMap<int, State> m_InteractableStates;

        public InteractableComponent(InteractionReference interaction)
        {
            m_InteractableStates = new UnsafeHashMap<int, State>(
                TypeHelper.Enum<InteractableState>.Length,
                AllocatorManager.Persistent
                );
            Set(InteractableState.Grounded,
                new State(
                    interaction.m_OnGrounded,
                    interaction.m_OnGroundedTriggerAction.ToFixedList64(),
                    new Fixed8<FixedConstAction>(interaction.m_OnGroundedConstAction.Select(t => new FixedConstAction(t)))
                    ));
            Set(InteractableState.Equiped,
                new State(
                    interaction.m_OnEquiped,
                    interaction.m_OnEquipedTriggerAction.ToFixedList64(),
                    new Fixed8<FixedConstAction>(interaction.m_OnEquipedConstAction.Select(t => new FixedConstAction(t)))
                    ));
            Set(InteractableState.Stored,
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

        public void Set(InteractableState type, State state)
        {
            m_InteractableStates[(int)type] = state;
        }
    }
}
