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
                    if (!IsMainthread())
                    {
                        AwaitForNotNull(ref m_Instance, ref m_IsEnforceOrder, EnforceOrder);
                        return m_Instance;
                    }
                    if (typeof(T) != typeof(CoreSystem) && !CoreSystem.Initialized)
                    {
                        CoreSystem.Instance.Initialize();
                    }

                    T ins;
                    var existing = FindObjectsOfType(typeof(T)) as T[];
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

#if UNITY_EDITOR
                    if (!string.IsNullOrEmpty(ins.DisplayName))
                    {
                        ins.gameObject.name = $"{ins.DisplayName} : StaticManager<{typeof(T).Name}>";
                    }
                    else ins.gameObject.name = $"Syadeu.{typeof(T).Name}";
#endif

                    if (ins.DontDestroy) DontDestroyOnLoad(ins.gameObject);
                    if (!Mono.SyadeuSettings.Instance.m_VisualizeObjects)
                    {
                        if (ins.HideInHierarchy) ins.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    }

                    ins.OnInitialize();

                    if (typeof(T) == typeof(CoreSystem)) System = ins as CoreSystem;
                    else
                    {
                        if (ins.DontDestroy)
                        {
                            CoreSystem.StaticManagers.Add(ins);
                            ins.transform.SetParent(System.transform);
                        }
                        else CoreSystem.InstanceManagers.Add(ins);
                    }

                    m_Instance = ins;
                    ins.OnStart();
                }
                return m_Instance;
            }
        }

        public virtual string DisplayName => null;
        public virtual bool DontDestroy => true;
        public virtual bool HideInHierarchy => true;

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
    }
}
