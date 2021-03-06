﻿using Syadeu.Mono;
using UnityEngine;

namespace Syadeu
{
    public abstract class MonoManager<T> : ManagerEntity, IStaticMonoManager 
        where T : Component, IStaticMonoManager
    {
        protected static CoreSystem System => CoreSystem.Instance;
        public SystemFlag Flag => SystemFlag.SubSystem;

        public static bool Initialized => m_Instance != null;

        internal static T m_Instance = null;
        public static T Instance => m_Instance;

        public virtual string DisplayName => null;
        public virtual bool DontDestroy => false;
        public virtual bool HideInHierarchy => false;
        public virtual bool ManualInitialize => false;

#if UNITY_EDITOR
        private System.Diagnostics.StackFrame m_InitLastStack = null;
#endif

        protected virtual void Awake()
        {
            if (!ManualInitialize) Initialize();
        }
        public void Initialize()
        {
            if (Initialized)
            {
#if UNITY_EDITOR
                throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{typeof(T).Name}은 {m_InitLastStack.GetFileName()}의 " +
                    $"{m_InitLastStack.GetMethod()}(:{m_InitLastStack.GetFileLineNumber()})에서 " +
                    $"이미 초기화되었는데 다시 초기화려합니다.");
#else
                throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{typeof(T).Name}은 이미 초기화되었는데 다시 초기화하려합니다.");
#endif
            }

#if UNITY_EDITOR
            var temp = new System.Diagnostics.StackTrace(true);
            m_InitLastStack = temp.GetFrame(temp.FrameCount - 1);

            if (!string.IsNullOrEmpty(DisplayName))
            {
                name = $"{DisplayName} : MonoManager<{typeof(T).Name}>";
            }
            else name = $"Syadeu.{typeof(T).Name}";
#endif

            if (DontDestroy) transform.SetParent(System.transform);

            if (!SyadeuSettings.Instance.m_VisualizeObjects)
            {
                if (HideInHierarchy) gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

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
