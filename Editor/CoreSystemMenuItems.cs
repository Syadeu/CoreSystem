using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syadeu;
using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Mono.Creature;
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
#if UNITY_ADDRESSABLES
        [MenuItem("CoreSystem/Edit Async Prefab List", priority = 3)]
        public static void AsyncPrefabListMenu()
        {
            Selection.activeObject = AsyncPrefabList.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif
        [MenuItem("CoreSystem/Edit Item Data List", priority = 3)]
        public static void ItemDataListMenu()
        {
            Selection.activeObject = ItemDataList.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        public static ItemDesigner m_ItemDesignerWindow;
        [MenuItem("CoreSystem/Designer/Item", priority = 100)]
        public static void ItemDesigner()
        {
            if (m_ItemDesignerWindow == null)
            {
                m_ItemDesignerWindow = GetWindow<ItemDesigner>();
                m_ItemDesignerWindow.titleContent = new GUIContent("Creature Window");
                //m_ItemDesignerWindow.minSize = new Vector2(620, m_CreatureWindow.minSize.y);
                //m_ItemDesignerWindow.maxSize = new Vector2(620, m_CreatureWindow.maxSize.y);
            }

            m_ItemDesignerWindow.Show();
        }

        public static CreatureSystemWindow m_CreatureWindow;
        [MenuItem("CoreSystem/Creature/Window", priority = 200)]
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

        [MenuItem("CoreSystem/Creature/Edit Creature Settings", priority = 201)]
        public static void CreatureSettingsWindow()
        {
            Selection.activeObject = CreatureSettings.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        static SQLiteWindow m_SQLiteWindow;
        [MenuItem("CoreSystem/SQLite/SQLite Window", priority = 300)]
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
        [MenuItem("CoreSystem/SQLite/Create Migration Data", priority = 301)]
        public static void EditSettings()
        {
            Selection.activeObject = Syadeu.Database.SQLiteMigrationData.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

#if CORESYSTEM_FMOD
        [MenuItem("Syadeu/FMOD/Edit FMOD Settings", priority = 400)]
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

    public sealed class CoreSystemGameobjectMenuItems
    {
        private static GameObject GetGameObject(string name, bool isStatic = false, params Type[] t)
        {
            GameObject obj;
            if (isStatic)
            {
                obj = GameObject.Find(name);
                if (obj != null) return obj;
            }

            obj = new GameObject(name);
            for (int i = 0; i < t.Length; i++)
            {
                obj.AddComponent(t[i]);
            }
            return obj;
        }

        [MenuItem("GameObject/CoreSystem/CoreSystem", false, 10)]
        public static void AddCoreSystem()
        {
            Selection.activeObject = GetGameObject("CoreSystem", true, typeof(CoreSystem));
        }

#if CORESYSTEM_UNITYAUDIO
        [MenuItem("GameObject/CoreSystem/Audio/Audio Source", false, 10)]
        public static void AddAudioSource()
        {
            Selection.activeObject = GetGameObject("UnityAudioSource", true, typeof(AudioSource), typeof(UnityAudioSource));
        }
#endif
    }
}
