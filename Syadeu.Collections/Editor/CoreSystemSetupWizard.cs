#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using Syadeu.Mono;
using System;
using UnityEditor;
using UnityEngine;

namespace Syadeu.Collections.Editor
{
    public sealed class CoreSystemSetupWizard : EditorWindow, IStaticInitializer
    {
        private static CoreSystemSetupWizard s_SetupWizard;
        private static SetupWizardMenuItem[] s_InternalMenuItems;

        public static SetupWizardMenuItem[] MenuItems
        {
            get
            {
                if (s_InternalMenuItems == null)
                {
                    Type[] menuItemTypes = TypeHelper.GetTypes(t => !t.IsAbstract && TypeHelper.TypeOf<SetupWizardMenuItem>.Type.IsAssignableFrom(t));
                    s_InternalMenuItems = new SetupWizardMenuItem[menuItemTypes.Length];
                    for (int i = 0; i < menuItemTypes.Length; i++)
                    {
                        s_InternalMenuItems[i] = (SetupWizardMenuItem)Activator.CreateInstance(menuItemTypes[i]);
                        s_InternalMenuItems[i].OnInitialize();
                    }
                    Array.Sort(s_InternalMenuItems);

                    //for (int i = 0; i < s_InternalMenuItems.Length; i++)
                    //{
                    //    if (!s_InternalMenuItems[i].Predicate())
                    //    {
                    //        CoreSystemMenuItems.CoreSystemSetupWizard();
                    //        break;
                    //    }
                    //}
                }
                return s_InternalMenuItems;
            }
        }

        static CoreSystemSetupWizard()
        {
            EditorApplication.delayCall -= Startup;
            EditorApplication.delayCall += Startup;
        }
        static void Startup()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            for (int i = 0; i < MenuItems.Length; i++)
            {
                if (!MenuItems[i].Predicate())
                {
                    Open();
                    return;
                }
            }

            if (!CoreSystemSettings.Instance.m_HideSetupWizard)
            {
                Open();
            }
        }

        public static void Open()
        {
            s_SetupWizard = (CoreSystemSetupWizard)GetWindow(TypeHelper.TypeOf<CoreSystemSetupWizard>.Type, true, "CoreSystem Setup Wizard");
            s_SetupWizard.ShowUtility();
            s_SetupWizard.minSize = new Vector2(600, 500);
            s_SetupWizard.maxSize = s_SetupWizard.minSize;
            var position = new Rect(Vector2.zero, s_SetupWizard.minSize);
            Vector2 screenCenter = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) / 2;
            position.center = screenCenter / EditorGUIUtility.pixelsPerPoint;
            s_SetupWizard.position = position;
        }

        private Texture2D m_EnableTexture;
        private Texture2D m_DisableTexture;

        GUIStyle titleStyle;
        GUIStyle iconStyle;

        private Rect m_CopyrightRect = new Rect(175, 475, 245, 20);

        private void OnEnable()
        {
            m_DisableTexture = CoreGUI.LoadAsset<Texture2D>("CrossYellow", "CoreSystemEditor");
            m_EnableTexture = CoreGUI.LoadAsset<Texture2D>("TickGreen", "CoreSystemEditor");

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            titleStyle.wordWrap = true;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            iconStyle = new GUIStyle();
            iconStyle.alignment = TextAnchor.MiddleCenter;

            m_SelectedToolbar = MenuItems[0];

            CoreSystemSettings.Instance.m_HideSetupWizard = true;
            EditorUtility.SetDirty(CoreSystemSettings.Instance);
        }
        private void OnGUI()
        {
            const string c_Copyrights = "Copyright 2022 Syadeu. All rights reserved.";

            GUILayout.Space(20);
            CoreGUI.Label("Setup", 30, TextAnchor.MiddleCenter);
            GUILayout.Space(10);
            CoreGUI.Line();
            GUILayout.Space(10);

            DrawToolbar();

            CoreGUI.Line();

            using (new CoreGUI.BoxBlock(Color.black))
            {
                m_SelectedToolbar.OnGUI();
            }

            EditorGUI.LabelField(m_CopyrightRect, HTMLString.String(c_Copyrights, 11), EditorStyleUtilities.CenterStyle);
        }

        public SetupWizardMenuItem SelectedToolbar => m_SelectedToolbar;

        private SetupWizardMenuItem m_SelectedToolbar;

        #region Toolbar

        private void DrawToolbar()
        {
            const float spacing = 50;

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.Space(spacing);

            for (int i = 0; i < MenuItems.Length; i++)
            {
                DrawToolbarButton(i, MenuItems[i].Name, MenuItems[i].Predicate());
            }

            GUILayout.Space(spacing);
            EditorGUILayout.EndHorizontal();
        }
        private void DrawToolbarButton(int i, string name, bool enable)
        {
            using (new CoreGUI.BoxBlock(i.Equals(m_SelectedToolbar) ? Color.black : Color.gray))
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
                if (GUILayout.Button(name, titleStyle))
                {
                    m_SelectedToolbar = MenuItems[i];
                }
                GUILayout.Label(enable ? m_EnableTexture : m_DisableTexture, iconStyle);
                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion
    }
}
