using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using System;
using Unity.Mathematics;
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
        private InputSystemSettings.InputActionContatiner[] m_InputActions;

        protected override PresentationResult OnInitialize()
        {
            m_InputActions = InputSystemSettings.Instance.m_InputActions;
            if (m_InputActions.Length == 0)
            {
                m_Enable = false;
                return PresentationResult.Warning("InputActions is not set. InputSystem will be offline.");
            }

            for (int i = 0; i < m_InputActions.Length; i++)
            {
                if (!m_InputActions[i].EnableAtStart) continue;

                m_InputActions[i].InputActions.Enable();
            }

            for (int i = 0; i < InputSystemSettings.Instance.m_AdditionalInputActions.Length; i++)
            {
                InputSystemSettings.CustomInputAction temp = InputSystemSettings.Instance.m_AdditionalInputActions[i];

                temp.InputAction.performed += (other) =>
                {
                    temp.ResponseActions.Execute(other);
                    temp.Actions.Execute();
                };
                temp.InputAction.Enable();
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
    public sealed class ParamActionFloat2Interaction : IInputInteraction
    {
        public Reference<ParamAction<float2>>[] Actions = Array.Empty<Reference<ParamAction<float2>>>();

        public void Process(ref InputInteractionContext context)
        {
            //if (context.timerHasExpired)
            //{
            //    context.Canceled();
            //    return;
            //}
            //context.action.
            switch (context.phase)
            {
                case InputActionPhase.Disabled:
                    break;
                case InputActionPhase.Waiting:
                    break;
                case InputActionPhase.Started:
                    break;
                case InputActionPhase.Performed:
                    float2 value = context.ReadValue<Vector2>();

                    Actions.Execute(value);
                    break;
                case InputActionPhase.Canceled:
                    break;
                default:
                    break;
            }
        }
        public void Reset()
        {
        }
    }

    //public sealed class ParamActionProcessor : InputProcessor<Vector2>
    //{
    //    public Reference<ParamAction<float2>>[] Actions = Array.Empty<Reference<ParamAction<float2>>>();

    //    public override Vector2 Process(Vector2 value, InputControl control)
    //    {
    //        Actions.
    //    }
    //}
}
