#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using Syadeu;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    public sealed class CoreSystemSetupWizard : EditorWindow
    {
        static CoreSystemSetupWizard()
        {
            EditorApplication.delayCall += Startup;
        }
        static void Startup()
        {
            if (!SyadeuSettings.Instance.m_HideSetupWizard)
            {
                CoreSystemMenuItems.CoreSystemSetupWizard();
            }
        }
        public enum ToolbarNames
        {
            Scene,
            Test1,
            Test2,
            Test3,
            Test4,
        }

        Texture2D tickTexture;
        Texture2D crossTexture;

        GUIStyle titleStyle;
        GUIStyle iconStyle;

        private void OnEnable()
        {
            crossTexture = EditorUtils.LoadAsset<Texture2D>("CrossYellow", "CoreSystemEditor");
            //tickTexture = EditorGUIUtility.Load("TickGreen.png") as Texture2D;
            tickTexture = EditorUtils.LoadAsset<Texture2D>("TickGreen", "CoreSystemEditor");
            //var objs = AssetDatabase.FindAssets("CrossYellow l:CoreSystemEditor t:texture2D");
            //$"{objs.Length}".ToLog();

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            titleStyle.wordWrap = true;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            iconStyle = new GUIStyle();
            iconStyle.alignment = TextAnchor.MiddleCenter;

            
        }
        private void OnGUI()
        {
            GUILayout.Space(20);
            EditorUtils.StringHeader("asd", 30, true);
            GUILayout.Space(10);
            EditorUtils.Line();
            GUILayout.Space(10);

            DrawToolbar();

            EditorGUILayout.BeginVertical(EditorUtils.Box, GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField("asdasdasdasdasd");

            EditorGUILayout.EndVertical();
        }

        public ToolbarNames SelectedToolbar => (ToolbarNames)m_SelectedToolbar;
        private void AddSetup(ToolbarNames toolbar, Func<bool> predictate) => m_IsSetupDone.Add(toolbar, predictate);

        #region Toolbar
        private int m_SelectedToolbar = 0;
        private readonly Dictionary<ToolbarNames, Func<bool>> m_IsSetupDone = new Dictionary<ToolbarNames, Func<bool>>();
        private void DrawToolbar()
        {
            const float spacing = 50;

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.Space(spacing);

            string[] toolbarNames = Enum.GetNames(typeof(ToolbarNames));
            for (int i = 0; i < toolbarNames.Length; i++)
            {
                bool done;
                if (m_IsSetupDone.ContainsKey((ToolbarNames)i))
                {
                    if (m_IsSetupDone[(ToolbarNames)i].Invoke()) done = true;
                    else done = false;
                }
                else done = true;
                DrawToolbarButton(i, toolbarNames[i], done);
            }

            GUILayout.Space(spacing);
            EditorGUILayout.EndHorizontal();
        }
        private void DrawToolbarButton(int i, string name, bool enable)
        {
            EditorGUILayout.BeginHorizontal(EditorUtils.Box, GUILayout.Height(22));
            if (GUILayout.Button(name, titleStyle))
            {
                m_SelectedToolbar = i;
            }
            GUILayout.Label(enable ? tickTexture : crossTexture, iconStyle);
            EditorGUILayout.EndHorizontal();
        }
        #endregion
    }
}
