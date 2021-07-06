using UnityEngine;

namespace Syadeu.Entities
{
    public abstract class SettingEntity : ScriptableObject
    {
        protected static bool IsMainthread()
            => CoreSystem.IsThisMainthread();
    }
}
