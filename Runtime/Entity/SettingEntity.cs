using UnityEngine;

namespace Syadeu
{
    public abstract class SettingEntity : ScriptableObject
    {
        protected static bool IsMainthread()
            => CoreSystem.IsThisMainthread();
    }
}
