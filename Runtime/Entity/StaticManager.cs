using Syadeu.Database;
using UnityEngine;

namespace Syadeu
{
    /// <summary>
    /// 객체 자동 생성 Static 매니저 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class StaticManager<T> : StaticManagerEntity, IStaticMonoManager where T : Component, IStaticMonoManager
    {
        public static bool Initialized => m_Instance != null;

        internal static T m_Instance;
        private static bool m_IsEnforceOrder;
        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    global::System.Type t = typeof(T);
#if UNITY_EDITOR
                    if (CoreSystem.s_BlockCreateInstance)
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                            $"종료 중에 StaticManager<{typeof(T).Name}> 인스턴스를 생성하려 합니다.");
                    }
                    if (IsMainthread() && !Application.isPlaying) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                        $"StaticManager<{typeof(T).Name}>의 인스턴스 객체는 플레이중에만 생성되거나 받아올 수 있습니다.");
#endif
                    if (!IsMainthread())
                    {
                        lock (ManagerLock)
                        {
                            AwaitForNotNull(ref m_Instance, ref m_IsEnforceOrder, EnforceOrder);
                            return m_Instance;
                        }
                    }
                    if (m_Instance != null) return m_Instance;

                    if (t != typeof(CoreSystem) && !CoreSystem.Initialized)
                    {
                        CoreSystem.Instance.Initialize(SystemFlag.MainSystem);
                        DontDestroyOnLoad(CoreSystem.Instance.gameObject);
                    }

                    T ins;
                    var existing = FindObjectsOfType(t) as T[];
                    if (existing.Length > 0)
                    {
                        for (int i = 1; i < existing.Length; i++)
                        {
                            if (existing[i] != null)
                            {
                                Destroy(existing[i]);
                            }
                        }

                        ins = existing[0];
                    }
                    else
                    {
                        GameObject obj = new GameObject();
                        ins = obj.AddComponent<T>();
                    }

                    ins.transform.position = Vector3.zero;
#if UNITY_EDITOR
                    if (!string.IsNullOrEmpty(ins.DisplayName))
                    {
                        ins.gameObject.name = $"{ins.DisplayName} : StaticManager<{typeof(T).Name}>";
                    }
                    else ins.gameObject.name = $"StaticManager.{typeof(T).Name}";
#endif

                    //if (ins.DontDestroy) DontDestroyOnLoad(ins.gameObject);
                    if (!Mono.SyadeuSettings.Instance.m_VisualizeObjects)
                    {
                        if (ins.HideInHierarchy) ins.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    }

                    ins.OnInitialize();

                    if (t == typeof(CoreSystem))
                    {
                        System = ins as CoreSystem;
                        DontDestroyOnLoad(ins.gameObject);
                    }
                    else
                    {
                        if (ins.DontDestroy)
                        {
                            CoreSystem.Instance.StaticManagers.Add(ins);
                            CoreSystem.InvokeManagerChanged();
                            ins.transform.SetParent(System.transform);
                        }
                        else
                        {
                            CoreSystem.Instance.InstanceManagers.Add(ins);
                            CoreSystem.InvokeManagerChanged();
                            if (InstanceGroupTr == null)
                            {
                                InstanceGroupTr = new GameObject("InstanceSystemGroup").transform;
                            }
                            ins.transform.SetParent(InstanceGroupTr);
                        }
                    }

                    ins.gameObject.isStatic = true;
                    m_Instance = ins;

                    ConfigLoader.LoadConfig(ins);
                    ins.OnStart();
                }
                return m_Instance;
            }
        }

        public virtual string DisplayName => null;
        public virtual bool DontDestroy => true;
        public virtual bool HideInHierarchy => true;
        public bool ManualInitialize => false;

        public bool Disposed { get; private set; } = false;

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        public virtual void OnInitialize() { }
        public virtual void OnStart() { }
        public virtual void Initialize(SystemFlag flag = SystemFlag.SubSystem)
        {
            Flag = flag;
        }

        private static void EnforceOrder()
        {
            Instance.Initialize();
        }

        public void Dispose()
        {
            Disposed = true;
            CoreSystem.Instance.StaticManagers.Remove(this);
            m_Instance = null;
        }
    }
}
