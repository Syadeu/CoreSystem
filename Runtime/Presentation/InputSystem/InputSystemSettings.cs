using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.Input
{
    public sealed class InputSystemSettings : StaticSettingEntity<InputSystemSettings>
    {
        public InputActionAsset m_InputActions = null;
    }
}
