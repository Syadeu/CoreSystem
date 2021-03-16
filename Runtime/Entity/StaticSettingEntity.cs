using UnityEngine;
using System.IO;
using Syadeu.Extensions.Logs;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public abstract class StaticSettingEntity<T> : SettingEntity, IStaticSetting 
        where T : ScriptableObject, IStaticSetting
    {
        public virtual bool RuntimeModifiable { get; } = false;

        private static T m_Instance;
        private static bool m_IsEnforceOrder;
        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    if (!IsMainthread())
                    {
                        StaticManagerEntity.AwaitForNotNull(ref m_Instance, ref m_IsEnforceOrder, EnforceOrder);
                        return m_Instance;
                    }

                    string path;
                    var customPathAtt = typeof(T).GetCustomAttribute<CustomStaticSettingAttribute>();
                    if (customPathAtt != null)
                    {
                        path = customPathAtt.CustomPath;
                    }
                    else path = "Syadeu";
#if UNITY_EDITOR
                    if (!Directory.Exists($"Assets/Resources/{path}"))
                    {
                        Directory.CreateDirectory($"Assets/Resources/{path}");
                    }
#endif

                    m_Instance = Resources.Load<T>($"{path}/" + typeof(T).Name);
                    if (m_Instance == null)
                    {
                        //$"LOG :: Creating new static setting<{typeof(T).Name}> asset".ToLog();
                        m_Instance = CreateInstance<T>();
                        m_Instance.name = $"Syadeu {typeof(T).Name} Setting Asset";

#if UNITY_EDITOR
                        AssetDatabase.CreateAsset(m_Instance, $"Assets/Resources/{path}/" + typeof(T).Name + ".asset");
#endif
                    }

#if UNITY_EDITOR
                    if (Application.isPlaying)
#endif
                    {
                        if (!(m_Instance as StaticSettingEntity<T>).RuntimeModifiable) m_Instance = Instantiate(m_Instance);
                    }

                    m_Instance.OnInitialized();
                }

                return m_Instance;
            }
        }

        public bool Initialized { get; private set; }

        public virtual void OnInitialized()
        {
            Initialized = true;
        }
        public virtual void Initialize() { }

        private static void EnforceOrder()
        {
            Instance.Initialize();
        }
    }
}
