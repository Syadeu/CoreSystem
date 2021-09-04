using Newtonsoft.Json;
using System;
using System.ComponentModel;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("ParamAction: Input Button Action")]
    public sealed class InputButtonParamAction : ParamAction<InputAction.CallbackContext>
    {
        [JsonProperty(Order = 0, PropertyName = "OnButtonDown")]
        private Reference<InstanceAction>[] m_OnButtonDown = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 1, PropertyName = "OnButtonUp")]
        private Reference<InstanceAction>[] m_OnButtonUp = Array.Empty<Reference<InstanceAction>>();

        protected override void OnExecute(InputAction.CallbackContext target)
        {
            if (!(target.control is ButtonControl button))
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "Input type is not match");
                return;
            }

            if (button.wasPressedThisFrame) m_OnButtonDown.Execute();
            else if (button.wasReleasedThisFrame) m_OnButtonUp.Execute();
        }
    }
}
