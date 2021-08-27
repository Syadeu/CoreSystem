using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Syadeu.Presentation.Input
{
    public sealed class InputSystem : PresentationSystemEntity<InputSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private bool m_Enable = true;
        private InputActionAsset m_InputActions;

        protected override PresentationResult OnInitialize()
        {
            m_InputActions = InputSystemSettings.Instance.m_InputActions;
            if (m_InputActions == null)
            {
                m_Enable = false;
                return PresentationResult.Warning("InputActions is not set. InputSystem will be offline.");
            }

            m_InputActions.Enable();
            //global::UnityEngine.InputSystem.InputSystem.RegisterInteraction()

            for (int i = 0; i < InputSystemSettings.Instance.m_AdditionalInputActions.Length; i++)
            {
                var temp = InputSystemSettings.Instance.m_AdditionalInputActions[i];

                InputSystemSettings.Instance.m_AdditionalInputActions[i].InputAction.performed += (other) =>
                {
                    temp.ResponseAction.Execute(other);
                };
            }

            return base.OnInitialize();
        }
        private void asdasd()
        {
            InputAction inputAction = new InputAction();
            inputAction.bindingMask = new InputBinding();

            //InputControlPath.TryFindControl<>(Gamepad.current, "leftStick/x");
        }

        public static InputControl ToControlType(ControlType controlType)
        {
            if (controlType == ControlType.Keyboard) return Keyboard.current;
            else if (controlType == ControlType.Mouse) return Mouse.current;
            else if (controlType == ControlType.Gamepad) return Gamepad.current;

            return null;
        }
    }

    public sealed class TestInputActionCallback : TParamAction<InputAction.CallbackContext>
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
    public sealed class MyInteraction : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {

        }
        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}
