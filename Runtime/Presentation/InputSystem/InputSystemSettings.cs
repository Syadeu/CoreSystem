using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Data;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.Input
{
    public sealed class InputSystemSettings : StaticSettingEntity<InputSystemSettings>
    {
        public InputActionAsset m_InputActions = null;

        public InputAction[] m_AdditionalInputActions = Array.Empty<InputAction>();
    }
}
