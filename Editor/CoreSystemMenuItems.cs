using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syadeu;
using Syadeu.Mono;
using UnityEditor;
using UnityEngine;

#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
using Syadeu.Mono.Audio;
#endif

namespace SyadeuEditor
{
    public sealed class CoreSystemMenuItems : EditorWindow
    {
        [MenuItem("CoreSystem/Edit Settings", priority = 1)]
        public static void SettingsMenu()
        {
            Selection.activeObject = SyadeuSettings.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        [MenuItem("CoreSystem/Edit Prefab List", priority = 2)]
        public static void PrefabListMenu()
        {
            Selection.activeObject = PrefabList.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        public static CreatureSystemWindow m_CreatureWindow;
        [MenuItem("CoreSystem/Creature/Window", priority = 100)]
        public static void CreatureWindow()
        {
            if (m_CreatureWindow == null)
            {
                m_CreatureWindow = GetWindow<CreatureSystemWindow>();
                m_CreatureWindow.titleContent = new GUIContent("Creature Window");
                m_CreatureWindow.minSize = new Vector2(620, m_CreatureWindow.minSize.y);
                m_CreatureWindow.maxSize = new Vector2(620, m_CreatureWindow.maxSize.y);
            }

            m_CreatureWindow.Show();
        }
        [MenuItem("CoreSystem/Creature/Edit Creature Settings", priority = 101)]
        public static void CreatureSettingsWindow()
        {
            Selection.activeObject = CreatureSettings.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        static SQLiteWindow m_SQLiteWindow;
        [MenuItem("CoreSystem/SQLite/SQLite Window", priority = 200)]
        public static void Initialize()
        {
            if (m_SQLiteWindow == null)
            {
                m_SQLiteWindow = GetWindow<SQLiteWindow>();
                m_SQLiteWindow.titleContent = new GUIContent("SQLite Viewer");
                m_SQLiteWindow.minSize = new Vector2(1200, 600);
            }

            m_SQLiteWindow.ShowUtility();
        }

#if CORESYSTEM_FMOD
        [MenuItem("Syadeu/FMOD/Edit FMOD Settings", priority = 300)]
        public static void FMODSettingsMenu()
        {
            Selection.activeObject = FMODSettings.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#elif CORESYSTEM_UNITYAUDIO
        [MenuItem("CoreSystem/Unity/AudioList")]
        public static void UnityAudioListMenu()
        {
            Selection.activeObject = UnityAudioList.Instance;
        }
#endif
    }
}
