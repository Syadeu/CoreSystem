using System;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Mono
{
    public class SDTypeSetting : StaticSettingEntity<SDTypeSetting>
    {
#if UNITY_EDITOR
        [MenuItem("Syadeu/SD Type Settings", priority = 200)]
        public static void MenuItem()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif

        [SerializeField] private List<SDType> m_Types = new List<SDType>();

        public List<SDType> Types => m_Types;

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                for (int i = 0; i < m_Types.Count; i++)
                {
                    m_Types[i].m_Guid = Guid.NewGuid();
                }
            }
        }

        public SDType GetSDType(Guid guid)
        {
            for (int i = 0; i < m_Types.Count; i++)
            {
                if (m_Types[i].m_Guid.Equals(guid))
                {
                    return m_Types[i];
                }
            }

            return null;
        }
    }
}
