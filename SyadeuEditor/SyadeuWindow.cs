using System.Collections.Generic;
using System.Linq;

using Syadeu;
using Syadeu.FMOD;
using Syadeu.Mono;

using UnityEngine;
using UnityEditor;

namespace SyadeuEditor
{
    public sealed class SyadeuWindow : EditorWindow
    {
        static SyadeuWindow window;

        [MenuItem("Syadeu/Syadeu Manager",  priority = 1)]
        public static void Initialize()
        {
            window = (SyadeuWindow)GetWindow(typeof(SyadeuWindow));
            window.Show();
        }

        Vector2 scrollPos = Vector2.zero;
        int windowIndex = 0;
        string[] windows = new string[] { "Global", "Generals" };

        private void OnGUI()
        {
            windowIndex = GUILayout.Toolbar(windowIndex, windows);

            scrollPos = GUILayout.BeginScrollView(scrollPos, true, true, GUILayout.Width(position.width), GUILayout.Height(position.height - 10));
            EditorGUI.BeginChangeCheck();
            switch (windowIndex)
            {
                case 0:
                    GlobalSettings();
                    break;
                case 1:
                    GeneralSettings();
                    break;
                default:
                    break;
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(SyadeuSettings.Instance);
                AssetDatabase.SaveAssets();
            }
            GUILayout.EndScrollView();
        }

        void GlobalSettings()
        {
            EditorUtils.StringHeader("Global Settings");
            EditorUtils.SectorLine();

            SyadeuSettings.Instance.m_VisualizeObjects = EditorGUILayout.ToggleLeft("Hierarchy에 표시", SyadeuSettings.Instance.m_VisualizeObjects);
        }
        void GeneralSettings()
        {
            EditorUtils.StringHeader("General Settings");
            EditorUtils.SectorLine();

            //SyadeuSettings.Instance.m_PMErrorAutoFix = EditorGUILayout.ToggleLeft("")
        }

        //void FMODGeneralInfo()
        //{
        //    EditorUtils.StringHeader("FMOD Generals");
        //    EditorUtils.SectorLine();
        //    EditorGUILayout.LabelField($"Current FMOD Objects: {FMODSound.InstanceCount}");

        //    int activatedCount = 0;
        //    for (int i = 0; i < FMODSound.Instances.Count; i++)
        //    {
        //        if (FMODSound.Instances[i].Activated)
        //        {
        //            activatedCount += 1;
        //        }
        //    }

        //    EditorGUILayout.LabelField($"Activated FMOD Objects: {activatedCount}");
        //    EditorUtils.SectorLine();

        //    if (EditorApplication.isPlaying)
        //    {
        //        Dictionary<string, int> FMODPlaylist = new Dictionary<string, int>();
        //        List<string> currentPlaylistNames = new List<string>();
        //        for (int i = 0; i < FMODSound.Playlist.Count; i++)
        //        {
        //            currentPlaylistNames.Add(FMODSound.Playlist[i].SoundGUID.EventPath);
        //            if (!FMODPlaylist.ContainsKey(FMODSound.Playlist[i].SoundGUID.EventPath))
        //            {
        //                FMODPlaylist.Add(FMODSound.Playlist[i].SoundGUID.EventPath, 1);
        //            }
        //            else
        //            {
        //                FMODPlaylist[FMODSound.Playlist[i].SoundGUID.EventPath] += 1;
        //            }
        //        }

        //        var list = FMODPlaylist.Keys.ToArray();
        //        for (int i = 0; i < list.Length; i++)
        //        {
        //            EditorGUILayout.LabelField($"{list[i]}: {FMODPlaylist[list[i]]}개 재생 중");
        //        }
        //    }
        //}
    }
}
