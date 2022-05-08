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
    public sealed class InputSystem : PresentationSystemEntity<InputSystem>,
        INotifySystemModule<InputGroupModule>
    {
        private const string c_KeyboardBinding = "<Keyboard>/{0}";
        private const string c_MouseBinding = "<Mouse>/{0}";

        public static InputGroup DefaultUIControls => "default-ui-controls";
        public static InputGroup DefaultIngameControls => "default-ingame-controls";

        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private bool m_EnableInput = false;
        private InputAction m_MousePositionInputAction;
        private readonly List<InputAction> m_CreatedInputActions = new List<InputAction>();
        private readonly Dictionary<UserActionType, UsereActionTypeHandle> m_CreatedUserActions = new Dictionary<UserActionType, UsereActionTypeHandle>();

        public sealed class UserActionHandle
        {
            private InputAction m_InputAction;

            public Predicate<InputAction.CallbackContext> executable;
            public event Action performed;

            internal UserActionHandle(InputAction inputAction)
            {
                m_InputAction = inputAction;

                m_InputAction.performed += M_InputAction_performed;
            }

            private void M_InputAction_performed(InputAction.CallbackContext obj)
            {
                if (executable != null && !executable.Invoke(obj)) return;

                performed?.Invoke();
            }

            public void ChangeBindings(InputAction inputAction)
            {
                for (int i = 0; i < inputAction.bindings.Count; i++)
                {
                    int index = m_InputAction.bindings.IndexOf(t => t.Equals(inputAction.bindings[i]));
                    if (index < 0)
                    {
                        m_InputAction.AddBinding(inputAction.bindings[i]);
                    }
                }
            }
            public void Execute() => performed?.Invoke();
        }
        private sealed class UsereActionTypeHandle
        {
            public UserActionHandle inputAction;
            //private bool opened;

            //public bool Opened => opened;

            public UsereActionTypeHandle(InputAction inputAction)
            {
                this.inputAction = new UserActionHandle(inputAction);
                //opened = false;

                //this.inputAction.performed += OnKeyHandler;
            }

            //private void OnKeyHandler()
            //{
            //    opened = !opened;
            //    $"{opened}".ToLog();
            //}
        }

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

        public event Action<Vector2> OnMousePositionChanged;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            ConsoleWindow.OnWindowOpened += ConsoleWindow_OnWindowOpened;

            m_MousePositionInputAction = new InputAction(
                "Mouse Position", InputActionType.Value,
                binding: "<Mouse>/position",
                expectedControlType: "Vector2");
            m_MousePositionInputAction.performed += OnMousePositionChangedHandler;
            m_MousePositionInputAction.Enable();

            {
                int userActionLength = TypeHelper.Enum<UserActionType>.Length;
                string binding; InputAction inputAction;
                for (int i = 1; i < userActionLength; i++)
                {
                    binding = GetUserActionKeyBindingString((UserActionType)i);
                    inputAction = new InputAction(
                        type: InputActionType.Button,
                        binding: binding);
                    inputAction.Enable();

                    m_CreatedUserActions.Add((UserActionType)i, new UsereActionTypeHandle(inputAction));
                }
            }

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);

            return base.OnInitialize();
        }

        private void OnMousePositionChangedHandler(InputAction.CallbackContext obj)
        {
            m_PrecalculatedCursorPosition = obj.ReadValue<Vector2>();
            if (m_RenderSystem != null)
            {
                m_PrecalculatedCursorRay = m_RenderSystem.ScreenPointToRay(new Unity.Mathematics.float3(m_PrecalculatedCursorPosition, 1));
            }

            OnMousePositionChanged?.Invoke(m_PrecalculatedCursorPosition);
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

            {
                int count = UserActionConstantDataProcessor.s_TempQueue.Count;
                for (int i = 1; i < count; i++)
                {
                    var item = UserActionConstantDataProcessor.s_TempQueue.Dequeue();
                    var inputAction = GetUserActionKeyBinding(item.m_UserActionType);
                    inputAction.performed += item.Execute;
                }
                UserActionConstantDataProcessor.s_TempQueue = null;
            }

            return base.OnStartPresentation();
        }
        protected override PresentationResult BeforePresentation()
        {
            m_PrecalculatedCursorPreseedInThisFrame = Mouse.current.leftButton.wasPressedThisFrame;

            return base.BeforePresentation();
        }

        #endregion

        #region Bindings

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
        /// <summary>
        /// 키보드 액션을 생성하여 반환합니다.
        /// </summary>
        /// <remarks>
        /// 사용자는 사용하기 위해 <see cref="InputAction.Enable"/> 을 호출해야합니다. 
        /// 사용이 모두 끝났으면 <see cref="RemoveBinding(InputAction)"/> 을 통해 반드시 제거해야합니다.
        /// </remarks>
        /// <param name="keyCode"></param>
        /// <param name="type"></param>
        /// <returns></returns>
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

        public UserActionHandle GetUserActionKeyBinding(UserActionType userActionType)
        {
            return m_CreatedUserActions[userActionType].inputAction;
        }
        //public bool IsUseractionKeyOpened(UserActionType userActionType) => m_CreatedUserActions[userActionType].Opened;

        private static string GetUserActionKeyBindingString(UserActionType userActionType)
        {
            const string c_Format = "Input_ActionType_{0}";

            string result;
            switch (userActionType)
            {
                case UserActionType.Interaction:
                    result = PlayerPrefs.GetString(
                        string.Format(c_Format, TypeHelper.Enum<UserActionType>.ToString(userActionType)),
                        "<Keyboard>/f"
                        );

                    break;
                case UserActionType.Inventory:
                    result = PlayerPrefs.GetString(
                        string.Format(c_Format, TypeHelper.Enum<UserActionType>.ToString(userActionType)),
                        "<Keyboard>/i"
                        );

                    break;
                default:
                    throw new NotImplementedException($"{userActionType}");
            }

            return result;
        }

        #endregion

        public static InputControl ToControlType(ControlType controlType)
        {
            if (controlType == ControlType.Keyboard) return Keyboard.current;
            else if (controlType == ControlType.Mouse) return Mouse.current;
            else if (controlType == ControlType.Gamepad) return Gamepad.current;

            return null;
        }

        public bool GetEnableInputGroup(InputGroup group)
        {
            return GetModule<InputGroupModule>().GetEnableInputGroup(group);
        }
        public void SetInputGroup(InputAction action, InputGroup group)
        {
            GetModule<InputGroupModule>().SetInputGroup(action, group);
        }
        public void SetEnableInputGroup(InputGroup group, bool enable)
        {
            GetModule<InputGroupModule>().SetEnableInputGroup(group, enable);
        }
    }

    internal sealed class InputGroupModule : PresentationSystemModule<InputSystem>
    {
        private Dictionary<InputGroup, List<InputAction>> m_GroupedActions = new Dictionary<InputGroup, List<InputAction>>();
        private Dictionary<InputAction, List<InputGroup>> m_LinkedGroup = new Dictionary<InputAction, List<InputGroup>>();
        private Dictionary<InputGroup, bool> m_Enabled = new Dictionary<InputGroup, bool>();

        public bool GetEnableInputGroup(InputGroup group)
        {
            if (!m_Enabled.TryGetValue(group, out var result)) return false;
            return result;
        }
        public void SetInputGroup(InputAction action, InputGroup group)
        {
            if (!m_GroupedActions.TryGetValue(group, out var list))
            {
                list = new List<InputAction>();
                m_GroupedActions.Add(group, list);
            }

            if (list.Contains(action))
            {
                "??".ToLogError();
            }
            else
            {
                list.Add(action);
            }

            if (!m_LinkedGroup.TryGetValue(action, out var links))
            {
                links = new List<InputGroup>();
                m_LinkedGroup.Add(action, links);
            }

            if (links.Contains(group))
            {
                "??".ToLogError();
            }
            else
            {
                links.Add(group);
            }
        }
        public void SetEnableInputGroup(InputGroup group, bool enable)
        {
            m_Enabled[group] = enable;

            if (!m_GroupedActions.TryGetValue(group, out var list)) return;

            if (enable)
            {
                foreach (var inputAction in list)
                {
                    inputAction.Enable();
                }
            }
            else
            {
                foreach (var inputAction in list)
                {
                    inputAction.Disable();
                }
            }
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
