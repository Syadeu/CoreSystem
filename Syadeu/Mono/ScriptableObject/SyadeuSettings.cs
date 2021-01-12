using Syadeu.Mono.Console;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Mono
{
    public partial class SyadeuSettings : StaticSettingEntity<SyadeuSettings>
    {
#if UNITY_EDITOR
        [MenuItem("Syadeu/Edit Settings", priority = 100)]
        public static void MenuItem()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif

        // Global System
        public bool m_VisualizeObjects = false;

        // PrefabManager
        public bool m_PMErrorAutoFix = true;

        // FMODManager
        public int m_FMODMemoryBlock = 512;

        // RenderManager
        public Vector3 m_ScreenOffset = new Vector3(.4f, .5f, 1);

        // Console
        public bool m_UseConsole = true;
        public bool m_UseOnlyDevelopmentBuild = false;
        public List<CommandDefinition> m_CommandDefinitions = new List<CommandDefinition>();
    }
}
 