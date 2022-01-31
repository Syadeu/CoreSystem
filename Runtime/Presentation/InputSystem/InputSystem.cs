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

using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using Syadeu.Collections;
using Syadeu.Mono;
using System.Collections.Generic;
using Syadeu.Presentation.Render;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

namespace Syadeu.Presentation.Input
{
    public sealed class InputSystem : PresentationSystemEntity<InputSystem>
    {
        private const string c_KeyboardBinding = "<Keyboard>/{0}";
        private const string c_MouseBinding = "<Mouse>/{0}";

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private bool m_EnableInput = false;
        private readonly List<InputAction> m_CreatedInputActions = new List<InputAction>();

        private Vector2 m_PrecalculatedCursorPosition;
        private Ray m_PrecalculatedCursorRay;
        private bool m_PrecalculatedCursorPreseedInThisFrame;

        private RenderSystem m_RenderSystem;

        public bool EnableInput
        {
            get => m_EnableInput;
            set
            {
                if (m_EnableInput.Equals(value)) return;

                InputSystemSettings settings = InputSystemSettings.Instance;
                for (int i = 0; i < settings.m_AdditionalInputActions.Length; i++)
                {
                    InputSystemSettings.CustomInputAction temp = settings.m_AdditionalInputActions[i];

                    if (value) temp.Enable();
                    else temp.Disable();
                }

                m_EnableInput = value;
            }
        }
        public Vector2 MousePosition => m_PrecalculatedCursorPosition;
        public Ray CursorRay => m_PrecalculatedCursorRay;
        public bool IsCursorPressedInThisFrame
        {
            get
            {
                return m_PrecalculatedCursorPreseedInThisFrame;
            }
        }

        protected override PresentationResult OnInitialize()
        {
            ConsoleWindow.OnWindowOpened += ConsoleWindow_OnWindowOpened;

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);

            return base.OnInitialize();
        }
        private void ConsoleWindow_OnWindowOpened(bool opened)
        {
            EnableInput = !opened;
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < m_CreatedInputActions.Count; i++)
            {
                m_CreatedInputActions[i].Disable();
                m_CreatedInputActions[i].Dispose();
            }
            m_CreatedInputActions.Clear();

            m_RenderSystem = null;
        }

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        protected override PresentationResult OnStartPresentation()
        {
            EnableInput = true;

            return base.OnStartPresentation();
        }
        protected override PresentationResult BeforePresentation()
        {
            m_PrecalculatedCursorPosition = Mouse.current.position.ReadValue();
            m_PrecalculatedCursorRay = m_RenderSystem.ScreenPointToRay(new Unity.Mathematics.float3(m_PrecalculatedCursorPosition, 1));
            m_PrecalculatedCursorPreseedInThisFrame = Mouse.current.leftButton.wasPressedThisFrame;

            return base.BeforePresentation();
        }

        public InputAction GetMouseButtonBinding(MouseButton mouseButton, InputActionType type)
        {
            const string
                c_LeftButton = "leftButton",
                c_RightButton = "rightButton",
                c_MiddleButton = "middleButton",
                c_ForwardButton = "forwardButton",
                c_BackButton = "backButton";

            InputAction action;
            switch (mouseButton)
            {
                default:
                case MouseButton.Left:
                    action = new InputAction(
                        binding: string.Format(c_MouseBinding, c_LeftButton),
                        type: type
                        );
                    break;
                case MouseButton.Right:
                    action = new InputAction(
                        binding: string.Format(c_MouseBinding, c_RightButton),
                        type: type
                        );
                    break;
                case MouseButton.Middle:
                    action = new InputAction(
                        binding: string.Format(c_MouseBinding, c_MiddleButton),
                        type: type
                        );
                    break;
                case MouseButton.Forward:
                    action = new InputAction(
                        binding: string.Format(c_MouseBinding, c_ForwardButton),
                        type: type
                        );
                    break;
                case MouseButton.Back:
                    action = new InputAction(
                        binding: string.Format(c_MouseBinding, c_BackButton),
                        type: type
                        );
                    break;
            }

            m_CreatedInputActions.Add(action);
            return action;
        }
        public InputAction GetKeyboardBinding(int number, bool isNumpad, InputActionType type)
        {
            const string c_Numpad = "numpad{0}";

            InputAction action;
            if (isNumpad)
            {
                action = new InputAction(binding:
                    string.Format(c_KeyboardBinding, string.Format(c_Numpad, number)),
                    type: type);
            }
            else
            {
                action = new InputAction(binding:
                    string.Format(c_KeyboardBinding, number),
                    type: type);
            }

            m_CreatedInputActions.Add(action);
            return action;
        }
        public InputAction GetKeyboardBinding(Key keyCode, InputActionType type)
        {
            InputAction action = new InputAction(binding:
                string.Format(c_KeyboardBinding, TypeHelper.Enum<Key>.ToString(keyCode)),
                type: type);

            m_CreatedInputActions.Add(action);
            return action;
        }
        public void RemoveBinding(InputAction action)
        {
            action.Disable();
            action.Dispose();

            m_CreatedInputActions.Remove(action);
        }

        public static InputControl ToControlType(ControlType controlType)
        {
            if (controlType == ControlType.Keyboard) return Keyboard.current;
            else if (controlType == ControlType.Mouse) return Mouse.current;
            else if (controlType == ControlType.Gamepad) return Gamepad.current;

            return null;
        }
    }

    public sealed class TestInputActionCallback : ParamAction<InputAction.CallbackContext>
    {
        protected override void OnExecute(InputAction.CallbackContext target)
        {
            "in".ToLog();
            base.OnExecute(target);
        }
    }

    public struct KeyboardBinding : IEquatable<KeyboardBinding>
    {
        public ControlType ControlType => ControlType.Keyboard;

        public Key Key;

        public KeyControl ToControl() => Keyboard.current[Key];
        public bool Equals(KeyboardBinding other) => Key.Equals(other.Key);
        public override string ToString() => InputControlPath.Combine(InputSystem.ToControlType(ControlType), TypeHelper.Enum<Key>.ToString(Key));
    }
    public struct MouseBinding
    {
        public ControlType ControlType => ControlType.Mouse;

        public MouseButton MouseButton;

        public InputControl ToControl() => InputControlPath.TryFindControl(Mouse.current, ToString());
        public override string ToString() => InputControlPath.Combine(InputSystem.ToControlType(ControlType), TypeHelper.Enum<MouseButton>.ToString(MouseButton));
    }
    public struct GamepadBinding
    {
        public ControlType ControlType => ControlType.Gamepad;

        public GamepadButton GamepadButton;

        public ButtonControl ToControl() => Gamepad.current[GamepadButton];
        public override string ToString() => InputControlPath.Combine(InputSystem.ToControlType(ControlType), TypeHelper.Enum<GamepadButton>.ToString(GamepadButton));
    }

    public enum ControlType
    {
        Keyboard,
        Mouse,
        Gamepad,
    }
}
