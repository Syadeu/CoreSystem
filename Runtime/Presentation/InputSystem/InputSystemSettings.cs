using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Data;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.Input
{
    public sealed class InputSystemSettings : StaticSettingEntity<InputSystemSettings>
    {
        [Serializable]
        public sealed class CustomInputAction
        {
            public InputAction InputAction;
            public bool Hold = false;

            [Header("Callback Actions")]
            public Reference<ParamAction<InputAction.CallbackContext>>[] ResponseActions
                = Array.Empty<Reference<ParamAction<InputAction.CallbackContext>>>();

            [Header("Actions")]
            public Reference<InstanceAction>[] Actions = Array.Empty<Reference<InstanceAction>>();

            public void Enable()
            {
                InputAction.performed += Performed;
                InputAction.canceled += Canceled;
                InputAction.Enable();
            }

            public void Disable()
            {
                InputAction.performed -= Performed;
                InputAction.canceled -= Canceled;
                InputAction.Disable();
            }

            private bool m_IsHolding = false;
            private void Performed(InputAction.CallbackContext other)
            {
                ResponseActions.Execute(other);
                Actions.Execute();

                if (Hold && !m_IsHolding)
                {
                    CoreSystem.StartUnityUpdate(this, WhileHold(other));
                    m_IsHolding = true;
                }
            }
            private IEnumerator WhileHold(InputAction.CallbackContext other)
            {
                while (InputAction.activeControl != null)
                {
                    ResponseActions.Execute(other);
                    Actions.Execute();

                    yield return null;
                }

                "Exit".ToLog();
            }
            private void Canceled(InputAction.CallbackContext obj)
            {
                m_IsHolding = false;
            }
        }

        public CustomInputAction[] m_AdditionalInputActions = Array.Empty<CustomInputAction>();
    }
}
