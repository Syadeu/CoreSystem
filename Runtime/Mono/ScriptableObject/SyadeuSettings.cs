using Syadeu.Mono.Console;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Mono
{
    public sealed class SyadeuSettings : StaticSettingEntity<SyadeuSettings>
    {
#if UNITY_EDITOR
        [MenuItem("Syadeu/Edit Settings", priority = 100)]
        public static void MenuItem()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif
        // Modules
        public UserTagNameModule m_UserTagNameModule;
        public CustomTagNameModule m_CustomTagNameModule;

        // Global System
        [Header("Global")]
        public bool m_VisualizeObjects = false;
        public bool m_CrashAfterException = false;
        public bool m_EnableAutoStaticInitialize = false;
        public string[] m_AutoInitializeTargetAssembly = new string[] { "Assembly-CSharp" };

        // PrefabManager
        [Header("Prefab Manager")]
        public bool m_PMErrorAutoFix = true;

        // FMODManager
        [Header("FMOD Manager")]
        public int m_FMODMemoryBlock = 512;

        // RenderManager
        [Header("Render Manager")]
        public Vector3 m_ScreenOffset = new Vector3(.4f, .5f, 1);

        // SQLiteDatabase
        [Header("SQLite Database")]
        public bool m_EnableQueryLog = false;

        // Console
        [Header("Console")]
        public bool m_UseConsole = true;
        public bool m_UseOnlyDevelopmentBuild = false;
        public int m_ConsoleFontSize = 15;
        //public ConsoleFlag m_ConsoleLogErrorTypes = ConsoleFlag.Error;
        [Tooltip("콘솔에 표시할 로그 타입을 지정합니다.")]
        // 콘솔에 표시할 로그 타입을 지정합니다.
        public ConsoleFlag m_ConsoleLogTypes = ConsoleFlag.Normal | ConsoleFlag.Error;
        public bool m_ConsoleLogWhenLogRecieved = false;
        [Tooltip("ConsoleLogWhenLogRecieved가 true이고 development build 일 경우에만 콘솔에 로그를 표시합니다.")]
        // ConsoleLogWhenLogRecieved가 true이고 development build 일 경우에만 콘솔에 로그를 표시합니다.
        public bool m_ConsoleLogOnlyIsDevelopment = true;
        public bool m_ConsoleThrowWhenErrorRecieved = true;
        public List<CommandDefinition> m_CommandDefinitions = new List<CommandDefinition>();
    }
}
 