using UnityEngine;

namespace Syadeu
{
    /// <summary>
    /// 객체 자동 생성 Static 매니저 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class StaticManager<T> : StaticManagerEntity, IStaticManager where T : Component
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
                        GameObject obj = new GameObject($"Syadeu.{typeof(T).Name}");
                        ins = obj.AddComponent<T>();
                    }

                    if (!string.IsNullOrEmpty((ins as IStaticManager).DisplayName))
                    {
                        ins.gameObject.name = (ins as IStaticManager).DisplayName + $" : StaticManager<{typeof(T).Name}>";
                    }

                    if ((ins as IStaticManager).DontDestroy) DontDestroyOnLoad(ins.gameObject);
                    if (!Mono.SyadeuSettings.Instance.m_VisualizeObjects)
                    {
                        if ((ins as IStaticManager).HideInHierarchy) ins.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    }

                    (ins as IStaticManager).OnInitialize();

                    if (typeof(T) == typeof(CoreSystem)) System = ins as CoreSystem;
                    else
                    {
                        if ((ins as IStaticManager).DontDestroy)
                        {
                            CoreSystem.Managers.Add(ins as ManagerEntity);
                            ins.transform.SetParent(System.transform);
                        }
                    }

                    m_Instance = ins;
                    (ins as IStaticManager).OnStart();
                }
                return m_Instance;
            }
        }

        public virtual string DisplayName => null;
        public virtual bool DontDestroy => true;
        public virtual bool HideInHierarchy { get => true; }

        public virtual void OnInitialize() { }
        public virtual void OnStart() { }
        public virtual void Initialize(SystemFlag flag = SystemFlag.SubSystem)
        {
            Flag = flag;
        }

        private static void EnforceOrder()
        {
            (Instance as IStaticManager).Initialize();
        }
    }
}
