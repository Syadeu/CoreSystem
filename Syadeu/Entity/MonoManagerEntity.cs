using System;
using UnityEngine;

namespace Syadeu
{
    /// <summary>
    /// 객체 사용자 생성 매니저 기본 클래스입니다.
    /// </summary>
    [Obsolete("퇴역됩니다 MonoManager<T>를 사용하세요")]
    public abstract class MonoManagerEntity : ManagerEntity
    {
        [Obsolete("퇴역됩니다")]
        public bool IsReady { get; protected set; }
    }

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
            if (!string.IsNullOrEmpty(DisplayName))
            {
                name = $"{typeof(T).Name} : MonoManager<{typeof(T).Name}>";
            }
            else name = $"Syadeu.{typeof(T).Name}";
            if (DontDestroy) transform.SetParent(System.transform);
            if (HideInHierarchy) gameObject.hideFlags = HideFlags.HideAndDontSave;

            OnInitialize();

            m_Instance = this as T;
            CoreSystem.InstanceManagers.Add(this);

            OnStart();
        }

        public static void ThreadAwaiter(int milliseconds)
            => StaticManagerEntity.ThreadAwaiter(milliseconds);

        public virtual void Initialize(SystemFlag flag = SystemFlag.SubSystem) { }
        public virtual void OnInitialize() { }
        public virtual void OnStart() { }
    }
}
