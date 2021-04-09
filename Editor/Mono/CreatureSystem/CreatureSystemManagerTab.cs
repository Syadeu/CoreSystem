using UnityEngine;

using Syadeu.Mono;

using UnityEditor;
using System.Linq;

namespace SyadeuEditor
{
    public sealed class CreatureSystemManagerTab : EditorStaticGUIContent<CreatureSystemManagerTab>
    {
        public CreatureSystemWindow Main => CoreSystemMenuItems.m_CreatureWindow;
        private CreatureManager Manager => Main.m_Manager;

        private Vector2 m_ContentScroll = Vector2.zero;


        protected override void OnInitialize()
        {
            if (Main == null) CoreSystemMenuItems.CreatureWindow();
        }

        protected override void OnGUIDraw()
        {
            //Selection.activeObject = null;

            EditorUtils.StringHeader("Creature Manager", 17);
            EditorUtils.SectorLine();

            m_ContentScroll = EditorGUILayout.BeginScrollView(m_ContentScroll, true, false, GUILayout.Height(Screen.height - 185));

            EditorGUILayout.Space();
            Main.CheckListConditions();
            for (int i = 0; i < Manager.m_CreatureSets.Count; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label($"{Manager.m_CreatureSets[i].m_DataIdx}", GUILayout.Width(15));
                var prefCol = GUI.color;
                GUI.color = Main.m_CreatureSetColor[i];
                if (GUILayout.Button(EditorUtils.String(Main.m_CreatureNameList[Manager.m_CreatureSets[i].m_DataIdx], StringColor.white), Main.m_BoxStyle, GUILayout.Width(360)))
                {
                    Main.m_ShowCreatureSet[i] = !Main.m_ShowCreatureSet[i];
                }
                GUI.color = prefCol;

                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    var temp = Manager.m_CreatureSets[i].m_SpawnRanges.ToList();
                    temp.Add(
                        new CreatureManager.SpawnRange
                        {
                            m_Center = Vector3.zero,
                            m_Count = 0,

                            m_Range = 10
                        });
                    Manager.m_CreatureSets[i].m_SpawnRanges = temp.ToArray();
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    if (Manager.m_CreatureSets[i].m_SpawnRanges.Length > 1)
                    {
                        var temp = Manager.m_CreatureSets[i].m_SpawnRanges.ToList();
                        temp.RemoveAt(temp.Count - 1);
                        Manager.m_CreatureSets[i].m_SpawnRanges = temp.ToArray();
                    }
                    else
                    {
                        Manager.m_CreatureSets.RemoveAt(i);
                        i--;
                        SceneView.lastActiveSceneView.Repaint();
                        continue;
                    }
                }

                GUILayout.EndHorizontal();

                if (Main.m_ShowCreatureSet[i])
                {
                    EditorGUILayout.Space();
                    for (int a = 0; a < Manager.m_CreatureSets[i].m_SpawnRanges.Length; a++)
                    {
                        EditorGUI.indentLevel += 3;

                        EditorGUILayout.BeginHorizontal();
                        EditorUtils.StringRich($"\t{Main.m_CreatureNameList[Manager.m_CreatureSets[i].m_DataIdx]}_{a}");

                        if (GUILayout.Button("View Target"))
                        {
                            Vector3 temp = Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center;
                            temp.y += 50;

                            Vector3 dir = Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center - SceneView.lastActiveSceneView.camera.transform.position;

                            temp -= dir.normalized * 1.5f * Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Range;

                            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

                            SceneView.lastActiveSceneView.LookAt(temp, rot);
                        }
                        if (GUILayout.Button("Remove"))
                        {
                            var temp = Manager.m_CreatureSets[i].m_SpawnRanges.ToList();
                            temp.RemoveAt(i);
                            Manager.m_CreatureSets[i].m_SpawnRanges = temp.ToArray();

                            break;
                        }

                        EditorGUILayout.EndHorizontal();

                        if (CreatureSettings.Instance.GetPrivateSet(Manager.m_CreatureSets[i].m_DataIdx) == null)
                        {

                        }
                        EditorGUI.BeginChangeCheck();
                        {
                            CreatureSystemSettingTab.GetCreaturePrivateSet(Manager.m_CreatureSets[i].m_DataIdx).m_PrefabIdx =
                            PrefabListEditor.DrawPrefabSelector(CreatureSystemSettingTab.GetCreaturePrivateSet(Manager.m_CreatureSets[i].m_DataIdx).m_PrefabIdx);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(CreatureSettings.Instance);
                        }

                        EditorGUI.BeginChangeCheck();
                        {
                            Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center
                            = EditorGUILayout.Vector3Field("", Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center);

                            Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Range
                            = EditorGUILayout.IntField("Range: ", Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Range);

                            Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Count
                                = EditorGUILayout.IntField("Count: ", Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Count);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            SceneView.lastActiveSceneView.Repaint();
                            EditorUtility.SetDirty(Manager);
                        }

                        EditorUtils.SectorLine();
                        EditorGUI.indentLevel -= 3;
                    }
                }

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }
        protected override void OnSceneGUIDraw(SceneView sceneView)
        {
            for (int i = 0; i < Manager.m_CreatureSets.Count; i++)
            {
                for (int a = 0; a < Manager.m_CreatureSets[i].m_SpawnRanges.Length; a++)
                {
                    float range = Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Range;
                    if (Main.m_CreatureSetColor.Length <= i) break;
                    Color temp = Main.m_CreatureSetColor[i];
                    temp.a = .5f;

                    EditorEntity.GLDrawCube(
                        Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center,
                        new Vector3(range, 10, range), in temp);

                    Handles.Label(Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center,
                        $"\t{Main.m_CreatureNameList[Manager.m_CreatureSets[i].m_DataIdx]}_{a}");

                    EditorGUI.BeginChangeCheck();
                    Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center =
                        Handles.PositionHandle(Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center,
                        Quaternion.identity);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center.y = 0;
                        Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center
                            = Vector3Int.RoundToInt(Manager.m_CreatureSets[i].m_SpawnRanges[a].m_Center);
                        EditorUtility.SetDirty(Manager);
                        Main.Repaint();
                    }
                }
            }
        }
    }

}