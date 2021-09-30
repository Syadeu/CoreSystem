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
            public string Name;

            [Space]
            //[SerializeField] private InputAction InputAction = Array.Empty<InputAction>();
            [SerializeField] private InputAction InputAction = null;
            [SerializeField] private bool Hold = false;

            [Header("Callback Actions")]
            [SerializeField] private Reference<ParamAction<InputAction.CallbackContext>>[] ResponseActions
                = Array.Empty<Reference<ParamAction<InputAction.CallbackContext>>>();

            [Header("Actions")]
            [SerializeField] private Reference<InstanceAction>[] Actions = Array.Empty<Reference<InstanceAction>>();

            public CustomInputAction()
            {
                InputAction = new InputAction("new");
            }

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
            private CoreRoutine m_LastCor;
            private void Performed(InputAction.CallbackContext other)
            {
                ResponseActions.Execute(other);
                Actions.Execute();

                if (Hold && !m_IsHolding)
                {
                    if (m_LastCor.IsValid()) CoreSystem.RemoveUnityUpdate(m_LastCor);

                    m_LastCor = CoreSystem.StartUnityUpdate(this, WhileHold(other));
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
            }
            private void Canceled(InputAction.CallbackContext obj)
            {
                ResponseActions.Execute(obj);
                Actions.Execute();

                m_IsHolding = false;
            }
        }

        public CustomInputAction[] m_AdditionalInputActions = Array.Empty<CustomInputAction>();
    }
}
