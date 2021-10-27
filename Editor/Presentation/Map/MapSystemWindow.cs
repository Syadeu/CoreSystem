using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using Syadeu.Presentation.Map;
using SyadeuEditor.Tree;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation.Map
{
    public sealed class MapSystemWindow : EditorWindowEntity<MapSystemWindow>
    {
        const string c_EditorOnly = "EditorOnly";

        protected override string DisplayName => "Map System";

        private MapDataLoader m_MapDataLoader;

        protected override void OnEnable()
        {
            m_MapDataLoader = new MapDataLoader();
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

            base.OnEnable();
        }
        private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                if (m_MapDataLoader != null)
                {
                    m_MapDataLoader.Dispose();
                    m_MapDataLoader = null;
                }
                m_MapDataLoader = new MapDataLoader();
            }
        }

        protected override void OnDisable()
        {
            Tools.hidden = false;

            if (m_MapDataLoader != null)
            {
                m_MapDataLoader.Dispose();
                m_MapDataLoader = null;
            }

            base.OnDisable();
        }
        private void OnDestroy()
        {
            if (m_MapDataLoader != null)
            {
                m_MapDataLoader.Dispose();
                m_MapDataLoader = null;
            }
        }
        private void OnValidate()
        {
            if (m_MapDataLoader != null)
            {
                m_MapDataLoader.Dispose();
                m_MapDataLoader = null;
            }
        }

        private void OnGUI()
        {
            using (new EditorUtilities.BoxBlock(Color.black))
            {
                EditorUtilities.StringHeader("Map System", 20, true);
            }
            GUILayout.Space(5);
            EditorUtilities.Line();

            //EditorGUI.BeginChangeCheck();
            //m_EnableEdit = EditorGUILayout.ToggleLeft("Enable Edit", m_EnableEdit);
            //if (EditorGUI.EndChangeCheck())
            //{
            //    if (!m_EnableEdit) Tools.hidden = false;
            //}
            //if (GUILayout.Button("Show Tools")) Tools.hidden = false;
            //EditorGUILayout.Space();

            //MapDataGUI();
            #region Scene data selector
            using (new EditorUtilities.BoxBlock(Color.gray))
            {
                using (new EditorGUI.DisabledGroupScope(m_SceneDataTarget == null))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save"))
                    {
                        EntityDataList.Instance.SaveData(m_SceneDataTarget);
                        if (m_GridMap != null)
                        {
                            EntityDataList.Instance.SaveData(m_GridMap.m_SceneDataGridAtt);
                        }
                    }
                    if (GUILayout.Button("Close"))
                    {
                        ResetSceneData();
                    }
                }
                ReferenceDrawer.DrawReferenceSelector("Scene data: ", (hash) =>
                {
                    if (hash.Equals(Hash.Empty))
                    {
                        ResetSceneData();
                        return;
                    }

                    m_SceneData = new Reference<SceneDataEntity>(hash);

                    if (m_SceneData.IsValid())
                    {
                        m_SceneDataTarget = m_SceneData.GetObject();

                        m_GridMap = new GridMapExtension(m_SceneDataTarget.GetAttribute<GridMapAttribute>());
                        SceneView.lastActiveSceneView.Repaint();
                    }
                }, m_SceneData, TypeHelper.TypeOf<SceneDataEntity>.Type);

                if (m_GridMap != null && m_GridMap.m_SceneDataGridAtt != null)
                {
                    using (new EditorUtilities.BoxBlock(Color.black))
                    {
                        m_GridMap.OnGUI();
                    }
                }
            }
            #endregion

            EditorUtilities.Line();

            m_MapDataLoader.OnGUI();

            if (Event.current.isKey)
            {
                if (Event.current.control && Event.current.keyCode == KeyCode.S)
                {
                    EntityDataList.Instance.SaveData();

                    CoreSystem.Logger.Log(Channel.Editor,
                        $"Map data Saved");
                }
            }
        }
        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                m_MapDataLoader.SelectObjects(Selection.gameObjects);
            }
            else if (m_MapDataLoader.SelectedGameObjects != null)
            {
                m_MapDataLoader.SelectObjects(null);
            }
        }
        protected override void OnSceneGUI(SceneView obj)
        {
            //MapDataSceneGUI(obj);
            m_GridMap?.OnSceneGUI(obj);
            m_MapDataLoader.OnSceneGUI();
        }

        #region Common
        private Transform m_PreviewFolder;
        const string c_EditInPlayingWarning = "Cannot edit data while playing";
        private void ResetSceneData()
        {
            m_SceneData = new Reference<SceneDataEntity>(Hash.Empty);
            m_SceneDataTarget = null;

            // GridMapAttribute
            m_GridMap?.Dispose();
            m_GridMap = null;

            SceneView.lastActiveSceneView.Repaint();
        }

        private void ResetPreviewFolder()
        {
            if (m_PreviewFolder != null) DestroyImmediate(m_PreviewFolder.gameObject);
            m_PreviewFolder = new GameObject("Preview").transform;
            m_PreviewFolder.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            m_PreviewFolder.gameObject.tag = c_EditorOnly;

            //m_PreviewObjects.Clear();
        }

        #endregion

        private Reference<SceneDataEntity> m_SceneData;
        private SceneDataEntity m_SceneDataTarget;

        #region GridMapAttribute
        private sealed class GridMapExtension : IDisposable
        {
            public readonly GridMapAttribute m_SceneDataGridAtt;
            public readonly BinaryGrid m_SceneDataGrid;
            public string[] m_GridLayerNames;

            public int m_SelectedGridLayer = 0;
            public GridMapAttribute.LayerInfo m_CurrentLayer = null;

            public bool m_EditLayer = false;

            private GridMapAttribute.LayerInfo SelectedLayer
            {
                get
                {
                    if (m_SelectedGridLayer == 0) return null;
                    return m_SceneDataGridAtt.m_Layers[m_SelectedGridLayer - 1];
                }
            }

            public GridMapExtension(GridMapAttribute att)
            {
                if (att == null) return;

                m_SceneDataGridAtt = att;
                m_SceneDataGrid = new BinaryGrid(m_SceneDataGridAtt.Center, m_SceneDataGridAtt.Size, m_SceneDataGridAtt.CellSize);

                ReloadLayers();
            }
            public void Dispose()
            {
                //m_SceneDataGrid?.Dispose();
            }

            private void ReloadLayers()
            {
                if (m_SceneDataGridAtt.m_Layers == null || m_SceneDataGridAtt.m_Layers.Length == 0)
                {
                    m_GridLayerNames = new string[] { "All" };
                }
                else
                {
                    var temp = m_SceneDataGridAtt.m_Layers.Select((other) => other.m_Name).ToList();
                    temp.Insert(0, "All");
                    m_GridLayerNames = temp.ToArray();
                }
            }
            private GridMapAttribute.LayerInfo GetLayer(int idx)
            {
                if (idx == 0 ||
                    m_SceneDataGridAtt == null ||
                    m_SceneDataGridAtt.m_Layers == null ||
                    m_SceneDataGridAtt.m_Layers.Length <= idx - 1) return null;

                return m_SceneDataGridAtt.m_Layers[idx - 1];
            }

            public void OnGUI()
            {
                EditorUtilities.StringRich("GridMapAttribute Extension", 13);

                #region Layer Selector
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Layer: ", GUILayout.Width(Screen.width * .25f));
                    m_SelectedGridLayer = EditorGUILayout.Popup(m_SelectedGridLayer, m_GridLayerNames);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    m_EditLayer = false;
                    m_CurrentLayer = GetLayer(m_SelectedGridLayer);
                    SceneView.lastActiveSceneView.Repaint();
                }

                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    var temp = m_SceneDataGridAtt.m_Layers.ToList();
                    temp.Add(new GridMapAttribute.LayerInfo());
                    m_SceneDataGridAtt.m_Layers = temp.ToArray();

                    ReloadLayers();

                    m_SelectedGridLayer = m_SceneDataGridAtt.m_Layers.Length;
                }
                EditorGUI.BeginDisabledGroup(m_SelectedGridLayer == 0);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    var temp = m_SceneDataGridAtt.m_Layers.ToList();
                    temp.RemoveAt(m_SelectedGridLayer - 1);
                    m_SceneDataGridAtt.m_Layers = temp.ToArray();

                    ReloadLayers();

                    if (m_SelectedGridLayer < 0) m_SelectedGridLayer = 0;
                    else if (m_SelectedGridLayer >= m_GridLayerNames.Length)
                    {
                        m_SelectedGridLayer = m_GridLayerNames.Length - 1;
                    }
                }

                m_EditLayer = GUILayout.Toggle(m_EditLayer, "E", EditorStyleUtilities.MiniButton, GUILayout.Width(20));
                if (m_EditLayer)
                {

                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                #endregion

                EditorUtilities.Line();

                // Layer Info
                #region Layer Info
                EditorGUI.indentLevel += 1;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtilities.StringRich($"Layer: {m_GridLayerNames[m_SelectedGridLayer]}", 13);
                    EditorGUI.BeginDisabledGroup(m_SelectedGridLayer == 0);
                    if (GUILayout.Button("Save", GUILayout.Width(50)))
                    {
                        EntityDataList.Instance.SaveData(m_SceneDataGridAtt);
                    }
                    if (GUILayout.Button("Clear", GUILayout.Width(50)))
                    {
                        SelectedLayer.m_Indices = Array.Empty<int>();
                        SceneView.lastActiveSceneView.Repaint();
                    }
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUI.indentLevel += 1;
                {
                    EditorGUI.BeginDisabledGroup(true);
                    if (m_SelectedGridLayer == 0)
                    {
                        int sum = 0;
                        for (int i = 0; i < m_SceneDataGridAtt.m_Layers.Length; i++)
                        {
                            sum += m_SceneDataGridAtt.m_Layers[i].m_Indices.Length;
                        }
                        EditorGUILayout.IntField("Indices", sum);
                    }
                    else EditorGUILayout.IntField("Indices", SelectedLayer.m_Indices.Length);
                    EditorGUI.EndDisabledGroup();

                    if (m_SelectedGridLayer != 0)
                    {
                        m_SceneDataGridAtt.m_Layers[m_SelectedGridLayer - 1].m_Inverse
                        = EditorGUILayout.ToggleLeft("Inverse", m_SceneDataGridAtt.m_Layers[m_SelectedGridLayer - 1].m_Inverse);
                    }
                }
                EditorGUI.indentLevel -= 1;
                EditorGUI.indentLevel -= 1;
                #endregion

                EditorUtilities.Line();
            }
            bool m_AddDrag = false;
            public void OnSceneGUI(SceneView obj)
            {
                const float c_LineThinkness = .05f;
                if (m_SceneDataGridAtt == null) return;

                #region Draw Grid & Layers

                m_SceneDataGrid.DrawGL(c_LineThinkness);
                Handles.DrawWireCube(m_SceneDataGrid.bounds.center, m_SceneDataGrid.size);

                if (m_SceneDataGridAtt.m_Layers == null)
                {
                    m_SceneDataGridAtt.m_Layers = Array.Empty<GridMapAttribute.LayerInfo>();
                }
                if (m_SceneDataGridAtt.m_Layers.Length > 0)
                {
                    float sizeHalf = m_SceneDataGrid.cellSize * .5f;

                    GL.PushMatrix();
                    GridExtensions.DefaultMaterial.SetPass(0);
                    Color color = Color.red;
                    color.a = .5f;
                    GL.Begin(GL.QUADS);
                    GL.Color(color);

                    if (m_CurrentLayer == null)
                    {
                        foreach (var item in m_SceneDataGridAtt.m_Layers)
                        {
                            for (int i = 0; i < item.m_Indices.Length; i++)
                            {
                                Vector3
                                    cellPos = m_SceneDataGrid.IndexToPosition(item.m_Indices[i]),
                                    p1 = new Vector3(cellPos.x - sizeHalf, cellPos.y + c_LineThinkness, cellPos.z - sizeHalf),
                                    p2 = new Vector3(cellPos.x - sizeHalf, cellPos.y + c_LineThinkness, cellPos.z + sizeHalf),
                                    p3 = new Vector3(cellPos.x + sizeHalf, cellPos.y + c_LineThinkness, cellPos.z + sizeHalf),
                                    p4 = new Vector3(cellPos.x + sizeHalf, cellPos.y + c_LineThinkness, cellPos.z - sizeHalf);

                                GL.Vertex(p1);
                                GL.Vertex(p2);
                                GL.Vertex(p3);
                                GL.Vertex(p4);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < m_CurrentLayer.m_Indices.Length; i++)
                        {
                            Vector3
                                cellPos = m_SceneDataGrid.IndexToPosition(m_CurrentLayer.m_Indices[i]),
                                p1 = new Vector3(cellPos.x - sizeHalf, cellPos.y + c_LineThinkness, cellPos.z - sizeHalf),
                                p2 = new Vector3(cellPos.x - sizeHalf, cellPos.y + c_LineThinkness, cellPos.z + sizeHalf),
                                p3 = new Vector3(cellPos.x + sizeHalf, cellPos.y + c_LineThinkness, cellPos.z + sizeHalf),
                                p4 = new Vector3(cellPos.x + sizeHalf, cellPos.y + c_LineThinkness, cellPos.z - sizeHalf);

                            GL.Vertex(p1);
                            GL.Vertex(p2);
                            GL.Vertex(p3);
                            GL.Vertex(p4);
                        }
                    }

                    GL.End();
                    GL.PopMatrix();
                }

                //for (int i = 0; i < m_SceneDataGrid.length; i++)
                //{
                //    float3 pos = m_SceneDataGrid.GetCellPosition(i);
                //    if (!EditorSceneUtils.IsDrawable(pos)) continue;

                //    Handles.Label(pos, $"{i}");
                //}
                #endregion

                if (!m_EditLayer || m_CurrentLayer == null) return;

                int mouseControlID = GUIUtility.GetControlID(FocusType.Passive);
                Ray ray; float3 point;
                switch (Event.current.GetTypeForControl(mouseControlID))
                {
                    case EventType.MouseDown:
                        GUIUtility.hotControl = mouseControlID;

                        if (Event.current.button == 0)
                        {
                            ray = EditorSceneUtils.GetMouseScreenRay();
                            if (m_SceneDataGrid.bounds.Intersect(ray, out _, out point))
                            {
                                int idx = m_SceneDataGrid.PositionToIndex(point);
                                List<int> tempList = m_CurrentLayer.m_Indices.ToList();

                                if (tempList.Contains(idx))
                                {
                                    tempList.Remove(idx);
                                    m_AddDrag = false;
                                }
                                else
                                {
                                    tempList.Add(idx);
                                    m_AddDrag = true;
                                }
                                m_CurrentLayer.m_Indices = tempList.ToArray();
                            }
                        }
                        else if (Event.current.button == 1)
                        {
                            m_EditLayer = false;
                        }

                        Event.current.Use();
                        break;
                    case EventType.MouseDrag:
                        GUIUtility.hotControl = mouseControlID;

                        ray = EditorSceneUtils.GetMouseScreenRay();
                        if (m_SceneDataGrid.bounds.Intersect(ray, out _, out point))
                        {
                            int idx = m_SceneDataGrid.PositionToIndex(point);
                            if (m_AddDrag)
                            {
                                if (!m_CurrentLayer.m_Indices.Contains(idx))
                                {
                                    List<int> tempList = m_CurrentLayer.m_Indices.ToList();
                                    tempList.Add(idx);
                                    m_CurrentLayer.m_Indices = tempList.ToArray();
                                }
                            }
                            else
                            {
                                if (m_CurrentLayer.m_Indices.Contains(idx))
                                {
                                    List<int> tempList = m_CurrentLayer.m_Indices.ToList();
                                    tempList.Remove(idx);
                                    m_CurrentLayer.m_Indices = tempList.ToArray();
                                }
                            }
                        }

                        Event.current.Use();
                        break;
                    case EventType.MouseUp:
                        GUIUtility.hotControl = 0;
                        if (Event.current.button == 0)
                        {

                        }

                        Event.current.Use();
                        break;

                }
            }
        }
        #endregion
        private GridMapExtension m_GridMap;
    }
}
