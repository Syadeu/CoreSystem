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
        [Header("Global")]
        public bool m_VisualizeObjects = false;
        public bool m_CrashAfterException = false;

        // PrefabManager
        [Header("Prefab Manager")]
        public bool m_PMErrorAutoFix = true;

        // FMODManager
        [Header("FMOD Manager")]
        public int m_FMODMemoryBlock = 512;

        // RenderManager
        [Header("Render Manager")]
        public Vector3 m_ScreenOffset = new Vector3(.4f, .5f, 1);

        // Console
        [Header("Console")]
        public bool m_UseConsole = true;
        public bool m_UseOnlyDevelopmentBuild = false;
        public int m_ConsoleFontSize = 15;
        public ConsoleFlag m_ConsoleLogErrorTypes = ConsoleFlag.Error;
        [Tooltip("콘솔에 로그를 표기할 타입을 지정합니다")]
        public ConsoleFlag m_ConsoleLogTypes = ConsoleFlag.Normal | ConsoleFlag.Error;
        public bool m_ConsoleLogWhenLogRecieved = false;
        [Tooltip("ConsoleLogWhenLogRecieved가 활성화 되있을때, 에디터이거나 개발 빌드일경우에만 콘솔에 로그를 표시합니다")]
        public bool m_ConsoleLogOnlyIsDevelopment = true;
        public bool m_ConsoleThrowWhenErrorRecieved = true;
        public List<CommandDefinition> m_CommandDefinitions = new List<CommandDefinition>();
    }
}
 