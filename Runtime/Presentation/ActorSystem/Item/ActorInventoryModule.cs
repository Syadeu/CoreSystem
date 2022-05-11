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
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using Syadeu.Presentation.Render.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using InputSystem = Syadeu.Presentation.Input.InputSystem;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorInventoryModule : PresentationSystemModule<ActorSystem>
    {
        private InputSystem m_InputSystem;
        private InputSystem.UserActionHandle m_InventoryKey;

        private SceneSystem m_SceneSystem;
        private RenderSystem m_RenderSystem;

        private InstanceID m_CurrentActor = InstanceID.Empty;
        private UIDocument m_UIDocument;

        public InstanceID CurrentActor => m_CurrentActor;
        public bool IsOpened => m_UIDocument != null;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);

            System.OnCurrentControlChanged += System_OnCurrentControlChanged;
        }

        protected override void OnShutDown()
        {
        }
        protected override void OnDispose()
        {
            m_InputSystem = null;
            m_SceneSystem = null;
            m_RenderSystem = null;
        }

        #region Binds

        private void Bind(InputSystem other)
        {
            m_InputSystem = other;

            m_InventoryKey = m_InputSystem.GetUserActionKeyBinding(UserActionType.Inventory);
            m_InventoryKey.executable = InventoryKeyExecutable;
            m_InventoryKey.performed += OnInventoryKeyEventHandler;
        }
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        #endregion

        #region Event Handlers

        private bool InventoryKeyExecutable(InputAction.CallbackContext ctx)
        {
            if (m_SceneSystem == null ||
               !m_SceneSystem.IsIngame ||
               System.CurrentControls.Count == 0) return false;

            InstanceID first = System.CurrentControls[0];
            if (!first.HasComponent<ActorControllerComponent>()) return false;

            return true;
        }
        private void System_OnCurrentControlChanged(IReadOnlyList<InstanceID> obj)
        {
            if (m_CurrentActor.IsEmpty()) return;

            if (!obj.Contains(m_CurrentActor))
            {
                DisableInventoryUI();
            }
        }
        private void OnInventoryKeyEventHandler()
        {
            if (IsOpened)
            {
                DisableInventoryUI();
                return;
            }

            InstanceID first = System.CurrentControls[0];
            EnableInventoryUI(first);
        }

        #endregion

        public void EnableInventoryUI(InstanceID actor)
        {
            if (!actor.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Actor({actor.GetEntity().Name}) doesn\'t have {nameof(ActorControllerComponent)}. This is not allowed.");
                return;
            }
            ActorControllerComponent component = actor.GetComponent<ActorControllerComponent>();
            if (!component.HasProvider<ActorInventoryProvider>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Actor({actor.GetEntity().Name}) doesn\'t have {nameof(ActorInventoryProvider)}.");
                return;
            }
            var invenProvider = component.GetProvider<ActorInventoryProvider>().Target;

            m_UIDocument = invenProvider.UIDocument;
            m_UIDocument.SetActive(true);

#if CORESYSTEM_DOTWEEN
            float originalHeight = m_UIDocument.rootVisualElement.resolvedStyle.height;
            m_UIDocument.rootVisualElement.style.height = 0;
            m_UIDocument.rootVisualElement.DOHeight(originalHeight, .5f);
#endif
            m_CurrentActor = actor;

            invenProvider.ExecuteOnInventoryOpened(actor);
        }
        public void DisableInventoryUI()
        {
            ActorControllerComponent component = m_CurrentActor.GetComponent<ActorControllerComponent>();
            var invenProvider = component.GetProvider<ActorInventoryProvider>().Target;

            invenProvider.ExecuteOnInventoryClosed(m_CurrentActor);

            m_CurrentActor = InstanceID.Empty;

            m_UIDocument.SetActive(false);
            m_UIDocument = null;
        }
    }
}
