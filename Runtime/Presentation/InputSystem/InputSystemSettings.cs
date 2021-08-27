﻿using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Data;
using System;
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

            [Header("Callback Actions")]
            public Reference<ParamAction<InputAction.CallbackContext>>[] ResponseActions
                = Array.Empty<Reference<ParamAction<InputAction.CallbackContext>>>();

            [Header("Actions")]
            public Reference<InstanceAction>[] Actions = Array.Empty<Reference<InstanceAction>>();
        }

        public InputActionAsset m_InputActions = null;

        public CustomInputAction[] m_AdditionalInputActions = Array.Empty<CustomInputAction>();
    }
}
