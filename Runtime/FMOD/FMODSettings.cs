﻿using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.FMOD
{
    public sealed class FMODSettings : StaticSettingEntity<FMODSettings>
    {
        public bool m_DisplayLogs = false;

        public List<SoundList> m_SoundLists = new List<SoundList>();
        public List<SoundRoom> m_SoundRooms = new List<SoundRoom>();
        public string[] m_LocalizeBankNames;

        //public Dictionary<int, SoundListGUID> SoundLists { get; private set; }
        //public Dictionary<int, SoundRoom> SoundRooms { get; private set; }
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

        //public override void OnInitialized()
        //{
        //    SoundLists = new Dictionary<int, SoundListGUID>();
        //    for (int i = 0; i < m_SoundLists.Count; i++)
        //    {
        //        SoundLists.Add(m_SoundLists[i].listIndex, new SoundListGUID(m_SoundLists[i]));
        //    }

        //    SoundRooms = new Dictionary<int, SoundRoom>();
        //    for (int i = 0; i < m_SoundRooms.Count; i++)
        //    {
        //        m_SoundRooms[i].Initialize(i);
        //        SoundRooms.Add(i, m_SoundRooms[i]);
        //    }
        //}
    }
}