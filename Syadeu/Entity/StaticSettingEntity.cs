﻿using UnityEngine;
using System.IO;
using Syadeu.Extentions.EditorUtils;
using System.Collections.Concurrent;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu
{
    public interface IStaticSetting
    {
        bool Initialized { get; }
        void OnInitialized();
        void Initialize();
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public abstract class StaticSettingEntity<T> : SettingEntity, IStaticSetting where T : ScriptableObject
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

                    m_Instance = Resources.Load<T>("Syadeu/" + typeof(T).Name);
                    if (m_Instance == null)
                    {
                        $"LOG :: Creating new static setting<{typeof(T).Name}> asset".ToLog();
                        m_Instance = CreateInstance<T>();
                        m_Instance.name = $"Syadeu {typeof(T).Name} Setting Asset";

#if UNITY_EDITOR
                        if (!Directory.Exists("Assets/Resources/Syadeu"))
                        {
                            AssetDatabase.CreateFolder("Assets/Resources", "Syadeu");
                        }
                        AssetDatabase.CreateAsset(m_Instance, "Assets/Resources/Syadeu/" + typeof(T).Name + ".asset");
#endif
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
