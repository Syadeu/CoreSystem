using UnityEngine;

namespace Syadeu
{
    public abstract class SettingEntity : ScriptableObject
    {
        protected static bool IsMainthread()
        {
            if (ManagerEntity.MainThread == null || System.Threading.Thread.CurrentThread == ManagerEntity.MainThread)
            {
                return true;
            }
            return false;
        }
    }
}
