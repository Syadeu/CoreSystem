using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.FMOD
{
    public sealed class FMODSettings : StaticSettingEntity<FMODSettings>
    {
        public bool m_DisplayLogs = false;

        [SerializeField] private List<SoundList> m_SoundLists = new List<SoundList>();
        [SerializeField] private List<SoundRoom> m_SoundRooms = new List<SoundRoom>();
        [SerializeField] private string[] m_LocalizeBankNames;

        public static List<SoundList> SoundLists { get { return Instance.m_SoundLists; } }
        public static List<SoundRoom> SoundRooms { get { return Instance.m_SoundRooms; } }
        public static string[] LocalizedBankNames { get { return Instance.m_LocalizeBankNames; } }

        const string SettingsAssetName = "FMODSystemSettings";


#if UNITY_EDITOR
        [MenuItem("Syadeu/FMOD/Edit FMOD Settings", priority = 3)]
        public static void EditSettings()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif


    }
}