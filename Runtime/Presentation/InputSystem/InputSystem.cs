﻿using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using Syadeu.Database;
using Syadeu.Mono;
using System.Collections.Generic;

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

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private bool m_EnableInput = false;
        private readonly List<InputAction> m_CreatedInputActions = new List<InputAction>();

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
        public Vector2 MousePosition => Mouse.current.position.ReadValue();

        protected override PresentationResult OnInitialize()
        {
            ConsoleWindow.OnWindowOpened += ConsoleWindow_OnWindowOpened;
            return base.OnInitialize();
        }
        private void ConsoleWindow_OnWindowOpened(bool opened)
        {
            EnableInput = !opened;
        }

        public override void OnDispose()
        {
            for (int i = 0; i < m_CreatedInputActions.Count; i++)
            {
                m_CreatedInputActions[i].Disable();
                m_CreatedInputActions[i].Dispose();
            }
            m_CreatedInputActions.Clear();
        }

        protected override PresentationResult OnStartPresentation()
        {
            EnableInput = true;

            return base.OnStartPresentation();
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
