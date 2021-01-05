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

                    var existing = FindObjectsOfType(typeof(T)) as T[];
                    foreach (var item in existing)
                    {
                        if (existing != null)
                        {
                            Destroy(item);
                        }
                    }

                    GameObject obj = new GameObject($"Syadeu.Extension.{typeof(T).Name}");
                    T ins = obj.AddComponent<T>();

                    if ((ins as IStaticManager).DontDestroy) DontDestroyOnLoad(obj);
                    if (!Mono.SyadeuSettings.Instance.m_VisualizeObjects)
                    {
                        obj.hideFlags = HideFlags.HideAndDontSave;
                    }

                    (ins as IStaticManager).OnInitialize();

                    if (typeof(T) == typeof(CoreSystem)) System = ins as CoreSystem;
                    else
                    {
                        CoreSystem.Managers.Add(ins as ManagerEntity);
                        obj.transform.SetParent(System.transform);
                    }

                    m_Instance = ins;
                    (ins as IStaticManager).OnStart();
                    //$"LOG :: {typeof(T).Name} has successfully loaded".ToLog();
                }
                return m_Instance;
            }
        }

        public virtual bool DontDestroy => true;

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
