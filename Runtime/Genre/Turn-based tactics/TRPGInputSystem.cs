#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Render;
using Syadeu.Presentation.TurnTable.UI;
using System.Collections.Generic;
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
        public override void OnDispose()
        {
            m_EventSystem.RemoveEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);

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

            m_EventSystem.AddEvent<TRPGShortcutUIPressedEvent>(TRPGShortcutUIPressedEventHandler);
        }
        private void Bind(Input.InputSystem other)
        {
            m_InputSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        #endregion

        #endregion

        #region EventHandlers

        private void TRPGShortcutUIPressedEventHandler(TRPGShortcutUIPressedEvent ev)
        {
            switch (ev.Shortcut)
            {
                case ShortcutType.Move:
                    break;
                case ShortcutType.Attack:
                    break;
                default:
                    break;
            }
        }

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
                m_InputSystem.RemoveBinding(action);
            }
        }

        #region Inner Classes

        private sealed class KeyboardInputs
        {
            private Input.InputSystem m_InputSystem;

            public InputAction m_Q, m_E;

            public KeyboardInputs(Input.InputSystem inputSystem)
            {
                m_InputSystem = inputSystem;

                m_Q = inputSystem.GetKeyboardBinding(Key.Q, InputActionType.Button);
                m_E = inputSystem.GetKeyboardBinding(Key.E, InputActionType.Button);
            }
        }
        private struct CameraControls
        {
            public static void RotateLeft(InputAction.CallbackContext obj)
            {
                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                TRPGCameraMovement cameraMovement = renderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

                cameraMovement.TargetOrientation += cameraMovement.RotationDegree;
            }
            public static void RotateRight(InputAction.CallbackContext obj)
            {
                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                TRPGCameraMovement cameraMovement = renderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

                cameraMovement.TargetOrientation -= cameraMovement.RotationDegree;
            }

            public static void SetNextTarget(InputAction.CallbackContext obj)
            {
                TRPGTurnTableSystem turnTableSystem = PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.System;

                ref var attComponent = ref turnTableSystem.CurrentTurn.GetComponent<TRPGActorAttackComponent>();
                int index = attComponent.GetTargetIndex() + 1;
                if (index >= attComponent.TargetCount)
                {
                    index = 0;
                }

                attComponent.SetTarget(index);

                RenderSystem renderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
                TRPGCameraMovement cameraMovement = renderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();

                cameraMovement.SetAim(turnTableSystem.CurrentTurn.As<IEntityData, IEntity>().transform, attComponent.GetTarget().GetEntity<IEntity>().transform);
            }
        }

        #endregion
    }
}