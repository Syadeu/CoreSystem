// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.IO;
using System.Reflection;

using Syadeu.Entities;
using Syadeu.Internal;
using Syadeu.Collections;

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
        private static object s_LockObj = new object();

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
                        lock (s_LockObj)
                        {
                            StaticManagerEntity.AwaitForNotNull(ref m_Instance, ref m_IsEnforceOrder, EnforceOrder);
                            return m_Instance;
                        }
                    }
                    if (m_Instance != null) return m_Instance;

                    string path;
                    var customPathAtt = TypeHelper.TypeOf<T>.Type.GetCustomAttribute<CustomStaticSettingAttribute>();
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
                        LogManager.LogOnDebug(TypeHelper.Enum<Channel>.ToString(Channel.Core),
                            ResultFlag.Normal,
                            $"Creating new static setting<{typeof(T).Name}> asset", true);
                        m_Instance = CreateInstance<T>();
                        m_Instance.name = $"Syadeu {TypeHelper.TypeOf<T>.Name} Setting Asset";

#if UNITY_EDITOR
                        AssetDatabase.CreateAsset(m_Instance, $"Assets/Resources/{path}/" + TypeHelper.TypeOf<T>.Name + ".asset");
#endif
                    }

#if UNITY_EDITOR
                    if (Application.isPlaying)
#endif
                    {
                        if (!(m_Instance as StaticSettingEntity<T>).RuntimeModifiable) m_Instance = Instantiate(m_Instance);
                    }

                    (m_Instance as StaticSettingEntity<T>).OnInitialize();
                    (m_Instance as StaticSettingEntity<T>).Initialized = true;
                }

                return m_Instance;
            }
        }

        public bool Initialized { get; private set; }
        /// <summary>
        /// <see langword="true"/> 일 경우, 런타임에서도 원본이 수정되어 저장됩니다.<br/>
        /// 기본값은 <see langword="false"/>입니다.
        /// </summary>
        public virtual bool RuntimeModifiable { get; } = false;

        protected virtual void OnDestroy()
        {
            m_Instance = null;
        }
        public virtual void OnInitialize() { }
        public void Initialize() { }

        private static void EnforceOrder()
        {
            Instance.Initialize();
        }
    }
}
