using Syadeu.Mono;
using UnityEngine;

namespace Syadeu
{
    public abstract class MonoManager<T> : ManagerEntity, IStaticMonoManager, IInitialize 
        where T : Component, IStaticMonoManager
    {
        protected static CoreSystem System => CoreSystem.Instance;
        public SystemFlag Flag => SystemFlag.SubSystem;

        public static bool Initialized { get; private set; }
        public static bool HasInstance => m_Instance != null;

        internal static T m_Instance = null;
        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        $"MonoManager를 상속받는 {typeof(T).Name}은 인스턴스가 자동으로 생성되지 않습니다.\n" +
                        $"먼저 컴포넌트를 빈 GameObject에 추가해주세요, 혹은 호출이 너무 일찍되었습니다. Awake에서 호출하지마세요.");
                }

                return m_Instance;
            }
        }

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
            else m_Instance = this as T;
        }
        public void Initialize()
        {
            var temp = new System.Diagnostics.StackTrace(true);
#if UNITY_EDITOR
            m_InitLastStack = temp.GetFrame(temp.FrameCount - 1);
#endif
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
            if (!IsMainthread())
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{typeof(T).Name}은 메인 유니티 스레드에서만 Initialize 될 수 있습니다.");
            }

#if UNITY_EDITOR

            if (!string.IsNullOrEmpty(DisplayName))
            {
                name = $"{DisplayName} : MonoManager<{typeof(T).Name}>";
            }
            else name = $"MonoManager.{typeof(T).Name}";
#endif
            if (!SyadeuSettings.Instance.m_VisualizeObjects)
            {
                if (HideInHierarchy) gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            OnInitialize();

            m_Instance = this as T;
            if (DontDestroy)
            {
                CoreSystem.StaticManagers.Add(this);
                transform.SetParent(System.transform);
            }
            else
            {
                CoreSystem.InstanceManagers.Add(this);
                if (InstanceGroupTr == null)
                {
                    InstanceGroupTr = new GameObject("InstanceSystemGroup").transform;
                }
                transform.SetParent(InstanceGroupTr);
            }

            OnStart();
            Initialized = true;
        }

        public static void ThreadAwaiter(int milliseconds)
            => StaticManagerEntity.ThreadAwaiter(milliseconds);

        public void Initialize(SystemFlag flag = SystemFlag.SubSystem) { }
        public virtual void OnInitialize() { }
        public virtual void OnStart() { }
    }
}
