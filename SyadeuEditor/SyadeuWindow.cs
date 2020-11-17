using UnityEngine;
using UnityEditor;

namespace Syadeu
{
    public sealed class SyadeuWindow : EditorWindow
    {
        static SyadeuWindow window;

        [MenuItem("Syadeu/Syadeu Manager")]
        public static void Initialize()
        {
            window = (SyadeuWindow)GetWindow(typeof(SyadeuWindow));
            window.Show();
        }

        Vector2 scrollPos = Vector2.zero;
        int windowIndex = 0;
        string[] windows = new string[] { "FMOD" };

        private void OnGUI()
        {
            windowIndex = GUILayout.Toolbar(windowIndex, windows);

            scrollPos = GUILayout.BeginScrollView(scrollPos, true, true, GUILayout.Width(position.width), GUILayout.Height(position.height - 10));
            switch (windowIndex)
            {
                case 0:
                    EditorGUILayout.LabelField($"Current FMOD Objects: {FMOD.FMODSound.InstanceCount}");

                    int activatedCount = 0;
                    for (int i = 0; i < FMOD.FMODSound.Instances.Count; i++)
                    {
                        if (FMOD.FMODSound.Instances[i].Activated)
                        {
                            activatedCount += 1;
                        }
                    }

                    EditorGUILayout.LabelField($"Activated FMOD Objects: {activatedCount}");
                    break;
                default:
                    break;
            }
            GUILayout.EndScrollView();
        }
    }
}
