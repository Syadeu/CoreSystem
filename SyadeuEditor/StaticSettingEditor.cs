using System.IO;

using Syadeu;
using Syadeu.Extentions.EditorUtils;

using UnityEngine;
using UnityEditor;

namespace SyadeuEditor
{
    [InitializeOnLoad]
    public abstract class StaticSettingEditor<T> : SettingEntity, IStaticSetting where T : ScriptableObject
    {
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

                    if (!Directory.Exists("Assets/Resources/Syadeu/Editor"))
                    {
                        Directory.CreateDirectory("Assets/Resources/Syadeu/Editor");
                    }

                    m_Instance = Resources.Load<T>("Syadeu/Editor" + typeof(T).Name);
                    if (m_Instance == null)
                    {
                        $"LOG :: Creating new static setting<{typeof(T).Name}> asset".ToLog();
                        m_Instance = CreateInstance<T>();
                        m_Instance.name = $"Syadeu {typeof(T).Name} Setting Asset";

                        if (!Directory.Exists("Assets/Resources/Syadeu/Editor"))
                        {
                            AssetDatabase.CreateFolder("Assets/Resources/Syadeu", "Editor");
                        }
                        AssetDatabase.CreateAsset(m_Instance, "Assets/Resources/Syadeu/Editor/" + typeof(T).Name + ".asset");
                    }
                }

                (m_Instance as IStaticSetting).OnInitialized();

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
            (Instance as IStaticSetting).Initialize();
        }
    }
}
