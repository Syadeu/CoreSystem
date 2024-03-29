﻿// Copyright 2021 Seung Ha Kim
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

using Syadeu.Collections;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Render;
using Syadeu.Presentation.TurnTable.UI;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    /// <summary>
    /// <see cref="TRPGAppCommonSystemGroup"/>
    /// </summary>
    public sealed class TRPGInputSystem : PresentationSystemEntity<TRPGInputSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly Dictionary<int, InputAction> m_ShortcutBindings = new Dictionary<int, InputAction>();

        private KeyboardInputs m_KeyboardInputs;

        private EventSystem m_EventSystem;
        private RenderSystem m_RenderSystem;
        private Input.InputSystem m_InputSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Input.InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnDispose()
        {
            m_EventSystem.RemoveEvent<OnAppStateChangedEvent>(OnAppStateChangedEventHandler);
            //m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);

            m_KeyboardInputs.Dispose();

            foreach (var item in m_ShortcutBindings)
            {
                m_InputSystem.RemoveBinding(item.Value);
            }
            m_ShortcutBindings.Clear();

            m_EventSystem = null;
            m_RenderSystem = null;
            m_InputSystem = null;
        }

        #region Binds

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnAppStateChangedEvent>(OnAppStateChangedEventHandler);
            //m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
        }
        private void Bind(Input.InputSystem other)
        {
            m_InputSystem = other;

            m_KeyboardInputs = new KeyboardInputs(m_InputSystem);
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        #endregion

        #endregion

        #region EventHandlers

        private void OnAppStateChangedEventHandler(OnAppStateChangedEvent ev)
        {
            switch (ev.State)
            {
                default:
                case OnAppStateChangedEvent.AppState.Main:
                    //break;
                case OnAppStateChangedEvent.AppState.Loading:
                    m_KeyboardInputs.Disable();

                    break;
                case OnAppStateChangedEvent.AppState.Game:
                    m_KeyboardInputs.SetIngame_Default();

                    break;
            }
        }
        //private void TRPGShortcutUIPressedEventHandler(TRPGShortcutUIPressedEvent ev)
        //{
        //    if (m_CurrentShortcut == ev.Shortcut)
        //    {
        //        m_KeyboardInputs.SetIngame_Default();

        //        m_CurrentShortcut = ShortcutType.None;
        //        return;
        //    }

        //    switch (ev.Shortcut)
        //    {
        //        case ShortcutType.Move:
        //            m_KeyboardInputs.SetIngame_Default();
        //            break;
        //        case ShortcutType.Attack:
        //            m_KeyboardInputs.SetIngame_TargetAim();

        //            break;
        //        default:
        //            break;
        //    }

        //    m_CurrentShortcut = ev.Shortcut;
        //}

        #endregion

        public void BindShortcut(TRPGShortcutUI shortcut)
        {
            InputAction inputAction = m_InputSystem.GetKeyboardBinding(shortcut.Index, false, InputActionType.Button);
            inputAction.performed += shortcut.OnKeyboardPressed;
            inputAction.Enable();

            m_ShortcutBindings.Add(shortcut.Index, inputAction);
        }
        public void UnbindShortcut(TRPGShortcutUI shortcut)
        {
            if (m_ShortcutBindings.TryGetValue(shortcut.Index, out var action))
            {
                action.performed -= shortcut.OnKeyboardPressed;
                m_InputSystem.RemoveBinding(action);

                m_ShortcutBindings.Remove(shortcut.Index);
            }
        }

        public void SetIngame_Default()
        {
            m_KeyboardInputs.SetIngame_Default();
        }
        public void SetIngame_TargetAim()
        {
            m_KeyboardInputs.SetIngame_TargetAim();
        }

        #region Inner Classes

        public sealed class KeyboardInputs : IDisposable
        {
            private Input.InputSystem m_InputSystem;

            public InputAction m_Q, m_E;

            public KeyboardInputs(Input.InputSystem inputSystem)
            {
                m_InputSystem = inputSystem;

                m_Q = inputSystem.GetKeyboardBinding(Key.Q, InputActionType.Button);
                m_E = inputSystem.GetKeyboardBinding(Key.E, InputActionType.Button);

                m_Q.Enable();
                m_E.Enable();

                inputSystem.SetInputGroup(m_Q, Input.InputSystem.DefaultIngameControls);
                inputSystem.SetInputGroup(m_E, Input.InputSystem.DefaultIngameControls);
            }

            public void Disable()
            {
                m_Q.performed -= CameraControls.RotateLeft;
                m_E.performed -= CameraControls.RotateRight;
                m_Q.performed -= CameraControls.SetPrevTarget;
                m_E.performed -= CameraControls.SetNextTarget;
            }
            public void SetIngame_Default()
            {
                Disable();

                m_Q.performed += CameraControls.RotateLeft;
                m_E.performed += CameraControls.RotateRight;
            }
            public void SetIngame_TargetAim()
            {
                Disable();

                m_Q.performed += CameraControls.SetPrevTarget;
                m_E.performed += CameraControls.SetNextTarget;
            }

            public void Dispose()
            {
                m_InputSystem.RemoveBinding(m_Q);
                m_InputSystem.RemoveBinding(m_E);

                m_Q = null;
                m_E = null;

                m_InputSystem = null;
            }
        }
        private struct CameraControls
        {
            public static void RotateLeft(InputAction.CallbackContext obj)
            {
                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                TRPGCameraMovement cameraMovement = renderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

                float3 temp = cameraMovement.TargetOrientation;
                temp.y -= cameraMovement.RotationDegree;

                cameraMovement.TargetOrientation = temp;
            }
            public static void RotateRight(InputAction.CallbackContext obj)
            {
                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                TRPGCameraMovement cameraMovement = renderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

                float3 temp = cameraMovement.TargetOrientation;
                temp.y += cameraMovement.RotationDegree;

                cameraMovement.TargetOrientation = temp;
            }

            public static void SetNextTarget(InputAction.CallbackContext obj) => SetTargetReletive(obj, 1);
            public static void SetPrevTarget(InputAction.CallbackContext obj) => SetTargetReletive(obj, -1);
            public static void SetTargetReletive(InputAction.CallbackContext obj, in int addictive)
            {
                TRPGTurnTableSystem turnTableSystem = PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.System;

                ref var attComponent = ref turnTableSystem.CurrentTurn.GetComponent<TRPGActorAttackComponent>();
                if (attComponent.TargetCount == 0)
                {
                    "no target".ToLog();
                    return;
                }

                int index = attComponent.GetTargetIndex() + addictive;
                if (index >= attComponent.TargetCount)
                {
                    index = 0;
                }
                else if (index < 0)
                {
                    index = attComponent.TargetCount - 1;
                }

                attComponent.SetTarget(index);

                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                TRPGCameraMovement cameraMovement = renderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

                cameraMovement.SetAim(turnTableSystem.CurrentTurn.transform, attComponent.GetTarget().GetEntity<IEntity>().transform);
            }
        }

        #endregion
    }
}