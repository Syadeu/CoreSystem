using UnityEngine;

namespace Syadeu
{
    public abstract class MonoManager<T> : ManagerEntity, IStaticMonoManager 
        where T : Component, IStaticMonoManager
    {
        protected static CoreSystem System => CoreSystem.Instance;
        public SystemFlag Flag => SystemFlag.SubSystem;

        public static bool Initialized => m_Instance != null;

        internal static T m_Instance;
        public static T Instance => m_Instance;

        public virtual string DisplayName => null;
        public virtual bool DontDestroy => false;
        public virtual bool HideInHierarchy => false;

        protected virtual void Awake()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(DisplayName))
            {
                name = $"{typeof(T).Name} : MonoManager<{typeof(T).Name}>";
            }
            else name = $"Syadeu.{typeof(T).Name}";
#endif

            if (DontDestroy) transform.SetParent(System.transform);
            if (HideInHierarchy) gameObject.hideFlags = HideFlags.HideAndDontSave;

            OnInitialize();

            m_Instance = this as T;
            if (DontDestroy) CoreSystem.StaticManagers.Add(this);
            else CoreSystem.InstanceManagers.Add(this);

            OnStart();
        }

        public static void ThreadAwaiter(int milliseconds)
            => StaticManagerEntity.ThreadAwaiter(milliseconds);

        public void Initialize(SystemFlag flag = SystemFlag.SubSystem) { }
        public virtual void OnInitialize() { }
        public virtual void OnStart() { }
    }
}
