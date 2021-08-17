﻿using Syadeu.Internal;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Syadeu.Presentation.Input
{
    public sealed class InputSystem : PresentationSystemEntity<InputSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        protected override PresentationResult OnInitialize()
        {
            //global::UnityEngine.InputSystem.InputSystem.RegisterInteraction()

            return base.OnInitialize();
        }
        private void asdasd()
        {
            InputAction inputAction = new InputAction();
            inputAction.bindingMask = new InputBinding();
        }

        public static InputControl ToControl(ControlType controlType)
        {
            if (controlType == ControlType.Keyboard) return Keyboard.current;
            else if (controlType == ControlType.Mouse) return Mouse.current;
            else if (controlType == ControlType.Gamepad) return Gamepad.current;

            return null;
        }
    }
    public struct KeyboardBinding : IEquatable<KeyboardBinding>
    {
        public ControlType ControlType => ControlType.Keyboard;

        public Key Key;

        public bool Equals(KeyboardBinding other) => Key.Equals(other.Key);
        public override string ToString() => InputControlPath.Combine(InputSystem.ToControl(ControlType), TypeHelper.Enum<Key>.ToString(Key));
    }
    public struct GamepadBinding
    {
        public ControlType ControlType => ControlType.Gamepad;

        public GamepadButton GamepadButton;

        public override string ToString() => InputControlPath.Combine(InputSystem.ToControl(ControlType), TypeHelper.Enum<GamepadButton>.ToString(GamepadButton));
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
