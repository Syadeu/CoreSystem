using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Syadeu.Mono;
using Syadeu.Mono.Creature;

using UnityEditor;
using System;
using Syadeu;
using System.Reflection;
using System.Linq;
using System.IO;

namespace SyadeuEditor
{
    public class CreatureSystemWindow : EditorWindow
    {
        public CreatureManager m_Manager;
        public string[] m_CreatureNameList = null;
        public int m_CreatureSelected = -1;
        public CreatureSettings.PrivateSet m_CreatureSelectedSet = null;

        private string[] m_ToolbarNames = new string[] { "Settings", "Creuture Manager" };
        public int m_ToolbarIdx = 0;
        private Rect m_ListRect = new Rect(-1, 144.5f, 0, 0);
        private Vector2 m_ListScroll = Vector2.zero;
        private Rect m_ListContentRect = new Rect(167, 144.5f, 460, 40);

        private Color
            m_SelectColor = new Color(0, 1, 0);
        public GUIStyle m_BoxStyle;

        // CreatureSettings
        public SerializedObject m_CreatureSettings;
        public SerializedProperty m_DepTypeName;
        public SerializedProperty m_DepSingleToneName;
        public SerializedProperty m_DepArrName;
        public SerializedProperty m_DepArrElementTypeName;
        public SerializedProperty m_DepDisplayName;

        // TargetList
        private SerializedObject m_TargetObj;
        private SerializedProperty m_TargetList;

        // CreatureManager
        public bool[] m_ShowCreatureSet = null;
        public Color[] m_CreatureSetColor = null;

        // Attributes
        public int m_SelectedDataClassName = 0;
        public CreatureDataAttribute m_SelectedDataAttribute = null;
        public string[] m_DataClassNames = new string[0];

        private void OnEnable()
        {
            m_Manager = FindObjectOfType<CreatureManager>();

            m_CreatureSettings = new SerializedObject(CreatureSettings.Instance);
            m_DepTypeName = m_CreatureSettings.FindProperty("m_DepTypeName");
            m_DepSingleToneName = m_CreatureSettings.FindProperty("m_DepSingleToneName");
            m_DepArrName = m_CreatureSettings.FindProperty("m_DepArrName");
            m_DepArrElementTypeName = m_CreatureSettings.FindProperty("m_DepArrElementTypeName");
            m_DepDisplayName = m_CreatureSettings.FindProperty("m_DepDisplayName");

            if (!string.IsNullOrEmpty(m_DepTypeName.stringValue) &&
                !string.IsNullOrEmpty(m_DepSingleToneName.stringValue) &&
                !string.IsNullOrEmpty(m_DepArrName.stringValue))
            {
                SetTargetList();
            }

            CheckListConditions();
            FindAttributeTargets();

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        public void SetTargetList()
        {
            const string NOTFOUND = "NOTFOUND";
            const string INVALID = "INVALID";

            Type t = GetTargetType(m_DepTypeName.stringValue);
            if (t == null) return;

            PropertyInfo propInfo = GetTargetPropertyInfo(t, m_DepSingleToneName.stringValue);

            object obj = propInfo?.GetGetMethod()?.Invoke(null, null);
            if (obj == null) return;

            m_TargetObj = new SerializedObject(obj as UnityEngine.Object);
            m_TargetList = m_TargetObj.FindProperty(m_DepArrName.stringValue);

            if (m_TargetList?.arraySize > 0)
            {
                m_DepArrElementTypeName.stringValue = m_TargetList.arrayElementType;
                m_CreatureNameList = new string[m_TargetList.arraySize];
                for (int i = 0; i < m_CreatureNameList.Length; i++)
                {
                    var temp = m_TargetList.GetArrayElementAtIndex(i).FindPropertyRelative(m_DepDisplayName.stringValue);

                    if (temp == null)
                    {
                        m_CreatureNameList[i] = NOTFOUND;
                    }
                    else if (!temp.type.Equals("string"))
                    {
                        m_CreatureNameList[i] = INVALID;
                    }
                    else
                    {
                        m_CreatureNameList[i] = temp.stringValue;
                    }
                }

                m_CreatureSettings.ApplyModifiedProperties();
            }
        }
        private Type GetTargetType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            Type t = Assembly.Load("Assembly-CSharp").GetType(name.Trim(), false, true);
            if (t == null)
            {
                t = Assembly.Load("Syadeu").GetType(name.Trim(), false, true);
            }
            return t;
        }
        private PropertyInfo GetTargetPropertyInfo(Type t, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            PropertyInfo propInfo = t.GetProperty(name.Trim());
            if (propInfo == null)
            {
                var genericT = typeof(StaticSettingEntity<>).MakeGenericType(t);
                if (t.BaseType.Equals(genericT))
                {
                    propInfo = genericT.GetProperty(name?.Trim());
                }
            }
            return propInfo;
        }
        private void FindAttributeTargets()
        {
            const string AssemblyCSharp = "Assembly-CSharp";

            Type[] types;
            try
            {
                types = Assembly.Load(AssemblyCSharp)
                .GetTypes()
                .Where(other => other.GetCustomAttribute<CreatureDataAttribute>() != null)
                .ToArray();
            }
            catch (FileNotFoundException)
            {
                types = new Type[0];
            }

            m_DataClassNames = new string[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                m_DataClassNames[i] = types[i].Name;
                if (m_DataClassNames[i].Equals(m_DepTypeName.stringValue))
                {
                    m_SelectedDataClassName = i;
                    m_SelectedDataAttribute = types[i].GetCustomAttribute<CreatureDataAttribute>();
                }
            }
        }
        private void OnHierarchyChange()
        {
            m_Manager = FindObjectOfType<CreatureManager>();

            Repaint();
        }
        private void OnProjectChange()
        {
            FindAttributeTargets();
        }
        private void OnGUI()
        {
            #region Init

            if (m_BoxStyle == null)
            {
                m_BoxStyle = new GUIStyle("Box")
                {
                    richText = true
                };
            }

            EditorUtils.StringHeader("Creature System");
            EditorUtils.SectorLine();

            EditorGUILayout.Space();
            if (m_Manager == null)
            {
                EditorUtils.StringRich("!! Creature Manager Not Found !!", true);
                EditorUtils.StringRich("Manager is not present in this scene or has been deactivated", 11, true);
                if (GUILayout.Button("Create Creature Manager"))
                {
                    GameObject obj = new GameObject("Creature Manager");
                    m_Manager = obj.AddComponent<CreatureManager>();
                    EditorUtility.SetDirty(m_Manager);
                }
            }
            if (m_Manager == null) return;

            #endregion

            EditorGUI.BeginChangeCheck();
            m_SelectedDataClassName = EditorGUILayout.Popup("List Class: ", m_SelectedDataClassName, m_DataClassNames);
            if (EditorGUI.EndChangeCheck())
            {
                m_DepTypeName.stringValue = m_DataClassNames[m_SelectedDataClassName];
                FindAttributeTargets();

                if (!string.IsNullOrEmpty(m_SelectedDataAttribute.SingleToneName))
                {
                    m_DepSingleToneName.stringValue = m_SelectedDataAttribute.SingleToneName;
                }
                if (!string.IsNullOrEmpty(m_SelectedDataAttribute.DataArrayName))
                {
                    m_DepArrName.stringValue = m_SelectedDataAttribute.DataArrayName;
                }
                
                SetTargetList();
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(m_DepSingleToneName, new GUIContent("List SingleTone Name: "));
            EditorGUILayout.PropertyField(m_DepArrName, new GUIContent("List Array Name: "));
            EditorGUI.EndDisabledGroup();

            EditorUtils.SectorLine();
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            m_ToolbarIdx = GUILayout.Toolbar(m_ToolbarIdx, m_ToolbarNames);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.lastActiveSceneView.Repaint();
            }
            EditorGUILayout.Space();

            BeginWindows();
            GUILayout.Window(0, m_ListRect, DrawCreatureList, "", "Box");
            switch (m_ToolbarIdx)
            {
                case 0:
                    GUILayout.Window(1, m_ListContentRect, (id) => CreatureSystemSettingTab.OnGUI(), "", "Box", GUILayout.Width(m_ListContentRect.width));
                    break;
                case 1:
                    GUILayout.Window(1, m_ListContentRect, (id) => CreatureSystemManagerTab.OnGUI(), "", "Box", GUILayout.Width(m_ListContentRect.width));
                    m_CreatureSelected = -1;

                    break;
                default:
                    break;
            }

            EndWindows();

            //EditorGUILayout.LabelField($"{Screen.width} :: {Screen.height}");

            if (m_CreatureSettings.hasModifiedProperties)
            {
                m_CreatureSettings.ApplyModifiedProperties();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            switch (m_ToolbarIdx)
            {
                case 1:
                    CreatureSystemManagerTab.OnSceneGUI(sceneView);
                    break;
                default:
                    break;
            }
        }
        private void OnDestroy()
        {
            SceneView.lastActiveSceneView.Repaint();
        }

        private void DrawCreatureList(int id)
        {
            m_ListScroll = EditorGUILayout.BeginScrollView(m_ListScroll, false, true, GUILayout.Width(165), GUILayout.Height(Screen.height - 150));
            EditorGUILayout.BeginVertical(GUILayout.Width(70));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(35);
            EditorGUILayout.LabelField("Creature List");
            EditorGUILayout.EndHorizontal();

            EditorUtils.SectorLine(140f);

            if (m_CreatureNameList == null || m_CreatureNameList.Length == 0)
            {
                EditorGUILayout.LabelField("NO DATA FOUND", m_BoxStyle, GUILayout.Width(140));
            }
            else
            {
                for (int i = 0; i < m_CreatureNameList.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{i}.", GUILayout.Width(15));

                    var prefCol = GUI.color;
                    if (m_CreatureSelected == i)
                    {
                        GUI.color = m_SelectColor;
                    }
                    if (GUILayout.Button(EditorUtils.String(m_CreatureNameList[i]), m_BoxStyle, GUILayout.Width(100)))
                    {
                        if (m_ToolbarIdx == 0)
                        {
                            SelectCreature(i);
                        }
                    }
                    GUI.color = prefCol;

                    EditorGUI.BeginDisabledGroup(m_ToolbarIdx == 0 || IsListHasCreatureIdx(i));
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        if (m_Manager.m_CreatureSets == null)
                        {
                            m_Manager.m_CreatureSets = new List<CreatureManager.CreatureSet>();
                        }
                        if (!CreatureSettings.Instance.HasPrivateSet(i))
                        {
                            $"해당 크리쳐에 대한 설정이 완료되지 않았습니다.".ToLogError();
                        }
                        else
                        {
                            m_Manager.m_CreatureSets.Add(new CreatureManager.CreatureSet
                            {
                                m_DataIdx = i,
                                m_PrefabIdx = CreatureSettings.Instance.GetPrivateSet(i).m_PrefabIdx,

                                m_SpawnRanges = new CreatureManager.SpawnRange[1]
                                {
                                    new CreatureManager.SpawnRange
                                    {
                                        m_Center = Vector3.zero,
                                        m_Count = 0,
                                        m_Range = 10
                                    }
                                }
                            });
                            m_Manager.m_CreatureSets.Sort();
                            SceneView.lastActiveSceneView.Repaint();
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorUtils.SectorLine(140f);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
        public void SelectCreature(int idx)
        {
            if (m_CreatureSelected == idx)
            {
                m_CreatureSelected = -1;
            }
            else m_CreatureSelected = idx;

            m_CreatureSelectedSet = CreatureSettings.Instance.GetPrivateSet(idx);
        }
        public void CheckListConditions()
        {
            if (m_Manager == null) return;

            if (m_ShowCreatureSet == null ||
                m_ShowCreatureSet.Length != m_Manager.m_CreatureSets.Count ||
                m_CreatureSetColor.Length != m_Manager.m_CreatureSets.Count)
            {
                m_ShowCreatureSet = new bool[m_Manager.m_CreatureSets.Count];
                m_CreatureSetColor = new Color[m_Manager.m_CreatureSets.Count];
                for (int i = 0; i < m_CreatureSetColor.Length; i++)
                {
                    m_CreatureSetColor[i] = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                }
            }
        }
        private bool IsListHasCreatureIdx(int idx)
        {
            for (int i = 0; i < m_Manager.m_CreatureSets.Count; i++)
            {
                if (m_Manager.m_CreatureSets[i].m_DataIdx == idx) return true;
            }
            return false;
        }
    }

    public sealed class CreatureSystemSettingTab : EditorStaticGUIContent<CreatureSystemSettingTab>
    {
        private CreatureSystemWindow Main => CoreSystemMenuItems.m_CreatureWindow;

        // CreatureSettings
        private SerializedProperty m_PrivateSets;

        protected override void OnInitialize()
        {
            if (Main == null) CoreSystemMenuItems.CreatureWindow();

            m_PrivateSets = Main.m_CreatureSettings.FindProperty("m_PrivateSets");
        }
        protected override void OnGUIDraw()
        {
            EditorUtils.StringHeader("General Settings", 17);
            EditorUtils.SectorLine();
            EditorGUI.indentLevel += 1;

            EditorGUILayout.Space();
            EditorUtils.StringHeader("Reflections", 14);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(Main.m_DepDisplayName, new GUIContent("List Display Name: ")/*, GUILayout.Width(150)*/);

            if (EditorGUI.EndChangeCheck())
            {
                Main.m_CreatureSettings.ApplyModifiedProperties();
                Main.SetTargetList();
            }

            EditorUtils.SectorLine();

            if (Main.m_CreatureSelected >= 0)
            {
                EditorUtils.StringHeader($"Creature Settings: {Main.m_CreatureNameList[Main.m_CreatureSelected]}", 14);
                DrawPrivateSet();
            }
            else
            {
                EditorUtils.StringHeader("Creature Settings: NONE", 14);
                EditorUtils.StringRich("Select Creature at the list", true);
            }

            EditorGUI.indentLevel -= 1;
        }

        private void DrawPrivateSet()
        {
            //Main.m_CreatureSelected
            if (Main.m_CreatureSelectedSet == null)
            {
                Main.m_CreatureSelectedSet = GetCreaturePrivateSet(Main.m_CreatureSelected);
            }
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.HelpBox("Loaded List From Prefab List", MessageType.Info);
            Main.m_CreatureSelectedSet.m_PrefabIdx = PrefabListEditor.DrawPrefabSelector(Main.m_CreatureSelectedSet.m_PrefabIdx);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(CreatureSettings.Instance);
            }
        }

        public static CreatureSettings.PrivateSet GetCreaturePrivateSet(int dataIdx)
        {
            var set = CreatureSettings.Instance.GetPrivateSet(dataIdx);
            if (set == null)
            {
                int idx = Instance.m_PrivateSets.arraySize;
                Instance.m_PrivateSets.InsertArrayElementAtIndex(idx);

                var dataIdxProp = Instance.m_PrivateSets.GetArrayElementAtIndex(idx).FindPropertyRelative("m_DataIdx");
                dataIdxProp.intValue = dataIdx;
                Instance.Main.m_CreatureSettings.ApplyModifiedProperties();

                set = CreatureSettings.Instance.GetPrivateSet(dataIdx);
            }

            return set;
        }
    }
}