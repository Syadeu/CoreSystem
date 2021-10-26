using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using SyadeuEditor.Tree;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
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

    public sealed class MapDataLoader : IDisposable
    {
        private Transform m_FolderInstance;
        public Transform Folder
        {
            get
            {
                if (m_FolderInstance == null)
                {
                    m_FolderInstance = new GameObject("MapData Preview").transform;
                }
                return m_FolderInstance;
            }
        }

        private Vector2 m_Scroll;

        private readonly List<Reference<MapDataEntityBase>> m_LoadedMapDataReference = new List<Reference<MapDataEntityBase>>();
        private readonly List<MapData> m_LoadedMapData = new List<MapData>();

        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();
        
        private MapData m_EditingMapData = null;

        private MapData m_SelectedMapData = null;
        private MapDataEntityBase.Object[] m_SelectedObjects = null;
        private GameObject[] m_SelectedGameObjects = null;

        private bool m_WasEditedMapDataSelector = false;

        public GameObject[] SelectedGameObjects => m_SelectedGameObjects;

        public MapDataLoader()
        {
            if (!EntityDataList.IsLoaded) EntityDataList.Instance.LoadData();
        }

        private MapDataEntityBase.Object GetMapDataObject(GameObject target, out MapData mapData)
        {
            mapData = null;
            if (target == null) return null;

            for (int i = 0; i < m_LoadedMapData.Count; i++)
            {
                MapDataEntityBase.Object temp = m_LoadedMapData[i][target];
                if (temp != null)
                {
                    mapData = m_LoadedMapData[i];
                    return temp;
                }
            }
            return null;
        }

        public void SelectObject(GameObject obj)
        {
            if (obj == null)
            {
                m_SelectedObjects = null;
                m_SelectedGameObjects = null;
                return;
            }

            m_SelectedObjects = new MapDataEntityBase.Object[1];
            m_SelectedObjects[0] = GetMapDataObject(obj, out m_SelectedMapData);
            m_SelectedMapData.SetDirty();

            m_SelectedGameObjects = m_SelectedObjects == null ? null : new GameObject[] { obj };
            Selection.activeGameObject = m_SelectedGameObjects[0];
        }
        public void SelectObjects(GameObject[] obj)
        {
            if (obj == null)
            {
                m_SelectedObjects = null;
                m_SelectedGameObjects = null;
                return;
            }

            m_SelectedObjects = new MapDataEntityBase.Object[obj.Length];
            for (int i = 0; i < obj.Length; i++)
            {
                m_SelectedObjects[i] = GetMapDataObject(obj[i], out m_SelectedMapData);
                if (m_SelectedMapData != null)
                {
                    m_SelectedMapData.SetDirty();
                }
            }

            //m_SelectedObjects = GetMapDataObject(obj, out m_SelectedMapData);
            m_SelectedGameObjects = m_SelectedObjects == null ? null : obj;
            //Selection.gameObjects = m_SelectedGameObjects;
        }

        public void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            using (new EditorUtilities.BoxBlock(Color.gray))
            {
                EditorGUI.BeginDisabledGroup(m_LoadedMapDataReference.Count == 0);
                if (GUILayout.Button("Save"))
                {
                    for (int i = 0; i < m_LoadedMapData.Count; i++)
                    {
                        if (!m_LoadedMapData[i].IsDirty) continue;

                        EntityDataList.Instance.SaveData(m_LoadedMapData[i].Entity);
                    }
                }
                EditorGUI.EndDisabledGroup();

                for (int i = 0; i < m_LoadedMapDataReference.Count; i++)
                {
                    int index = i;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawMapDataSelector(m_LoadedMapDataReference[index], (other) =>
                        {
                            if (m_EditingMapData != null && m_EditingMapData.Equals(m_LoadedMapData[index]))
                            {
                                m_EditingMapData = null;
                            }

                            if (other.IsEmpty() || !other.IsValid())
                            {
                                m_LoadedMapDataReference.RemoveAt(index);

                                if (m_LoadedMapData[index] != null)
                                {
                                    m_LoadedMapData[index].Dispose();
                                    m_LoadedMapData.RemoveAt(index);
                                }
                            }
                            else
                            {
                                m_LoadedMapDataReference[index] = other;

                                if (m_LoadedMapData[index] != null)
                                {
                                    m_LoadedMapData[index].Dispose();
                                }
                                m_LoadedMapData[index] = new MapData(Folder, other);
                            }

                            m_WasEditedMapDataSelector = true;
                        });

                        bool isEditing = false;
                        if (m_EditingMapData != null && m_EditingMapData.Equals(m_LoadedMapData[index]))
                        {
                            isEditing = true;
                        }

                        EditorGUI.BeginChangeCheck();
                        isEditing = GUILayout.Toggle(isEditing, "E", EditorStyleUtilities.MiniButton, GUILayout.Width(20));
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (isEditing) m_EditingMapData = m_LoadedMapData[index];
                            else
                            {
                                if (m_EditingMapData != null && m_EditingMapData.Equals(m_LoadedMapData[index]))
                                {
                                    m_EditingMapData = null;
                                }
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            if (m_EditingMapData != null && m_EditingMapData.Equals(m_LoadedMapData[index]))
                            {
                                m_EditingMapData = null;
                            }

                            m_LoadedMapDataReference.RemoveAt(index);

                            if (m_LoadedMapData[index] != null)
                            {
                                m_LoadedMapData[index].Dispose();
                                m_LoadedMapData.RemoveAt(index);
                            }

                            if (m_LoadedMapData.Count == 0)
                            {
                                SelectObjects(null);

                                UnityEngine.Object.DestroyImmediate(m_FolderInstance.gameObject);
                                m_FolderInstance = null;
                            }

                            i--;
                            continue;
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawMapDataSelector(Reference<MapDataEntityBase>.Empty, (other) =>
                    {
                        if (other.IsEmpty() || !other.IsValid())
                        {
                            "not valid".ToLog();
                            return;
                        }

                        if (m_LoadedMapDataReference.Contains(other))
                        {
                            "cannot load that already loaded".ToLog();
                            return;
                        }

                        m_LoadedMapDataReference.Add(other);
                        m_LoadedMapData.Add(new MapData(Folder, other));

                        m_WasEditedMapDataSelector = true;
                    });

                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Toggle(false, "E", EditorStyleUtilities.MiniButton, GUILayout.Width(20));
                    GUILayout.Button("-", GUILayout.Width(20));
                    EditorGUI.EndDisabledGroup();
                }

                if (m_WasEditedMapDataSelector)
                {
                    m_CreatedObjects.Clear();
                    for (int i = 0; i < m_LoadedMapData.Count; i++)
                    {
                        m_CreatedObjects.AddRange(m_LoadedMapData[i].CreatedObjects);
                    }

                    m_WasEditedMapDataSelector = false;
                }
            }

            EditorUtilities.Line();

            using (new EditorUtilities.BoxBlock(Color.gray))
            {
                EditorUtilities.StringRich("Lightmapping", 13);
                LightingSettings lightSettings = Lightmapping.GetLightingSettingsForScene(EditorSceneManager.GetActiveScene());
                EditorGUILayout.ObjectField("Light Settings", lightSettings, typeof(LightingSettings), false);

                //if (GUILayout.Button("Bake"))
                //{
                //    m_FolderInstance.gameObject.hideFlags = HideFlags.None;

                //    if (Lightmapping.isRunning)
                //    {
                //        Lightmapping.ForceStop();
                //        Lightmapping.Cancel();
                //    }

                //    if (m_LoadedMapData.Count != 0)
                //    {
                //        Lightmapping.ClearLightingDataAsset();

                //        Lightmapping.lightingSettings = lightSettings;
                //        Lightmapping.BakeAsync();
                //    }
                //    else
                //    {
                //        "no loaded map data found".ToLog();
                //    }
                //}
            }

            EditorGUILayout.EndScrollView();
        }

        public void OnSceneGUI()
        {
            if (m_LoadedMapData.Count == 0) return;

            #region Mouse Event

            int mouseControlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (Event.current.GetTypeForControl(mouseControlID))
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        //if (m_SelectedObject == null)
                        //{
                        //    GUIUtility.hotControl = mouseControlID;

                        //    GameObject obj = HandleUtility.PickGameObject(Event.current.mousePosition, true, null, null);

                        //    m_SelectedObject = GetMapDataObject(obj, out m_SelectedMapData);
                        //    m_SelectedGameObject = m_SelectedObject == null ? null : obj;

                        //    //if (m_SelectedObject != null) $"{m_SelectedObject.name}".ToLog();

                        //    Event.current.Use();
                        //}
                    }
                    else if (Event.current.button == 1)
                    {
                        //if (m_SelectedObject != null)
                        //{
                        //    GUIUtility.hotControl = mouseControlID;

                        //    GenericMenu menu = new GenericMenu();
                        //    menu.AddDisabledItem(new GUIContent(m_SelectedObject.m_Object.GetObject().Name));
                        //    menu.AddSeparator(string.Empty);

                        //    menu.AddItem(new GUIContent("Open in Window"), false, () =>
                        //    {
                        //        EntityWindow.Instance.Select(m_SelectedObject.m_Object);
                        //    });

                        //    menu.ShowAsContext();

                        //    Event.current.Use();
                        //}
                    }
                    else if (Event.current.button == 2)
                    {
                        if (m_EditingMapData == null)
                        {
                            "no editting map data".ToLog();
                            return;
                        }

                        GUIUtility.hotControl = mouseControlID;

                        m_SelectedMapData = null;
                        m_SelectedObjects = null;
                        m_SelectedGameObjects = null;

                        #region Draw Object Creation PopupWindow

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;
                        Vector3 pos = EditorSceneUtils.GetMouseScreenPos();

                        var list = EntityDataList.Instance.m_Objects.Where((other) => other.Value is EntityBase).Select((other) => (EntityBase)other.Value).ToArray();

                        PopupWindow.Show(rect, SelectorPopup<Hash, EntityBase>.GetWindow
                            (
                            list: list,
                            setter: (hash) =>
                            {
                                if (hash.IsEmpty())
                                {
                                    return;
                                }

                                Reference<EntityBase> refobj = new Reference<EntityBase>(hash);
                                MapDataEntityBase.Object obj = new MapDataEntityBase.Object
                                {
                                    m_Object = refobj,
                                    m_Translation = pos,
                                    m_Rotation = quaternion.identity,
                                    m_Scale = 1
                                };

                                //m_SelectedGameObject = m_EditingMapData.Add(obj);
                                //m_SelectedObject = GetMapDataObject(m_SelectedGameObject, out m_SelectedMapData);
                                SelectObject(m_EditingMapData.Add(obj));
                                m_WasEditedMapDataSelector = true;
                                //Repaint();
                            },
                            getter: (other) => other.Hash,
                            noneValue: Hash.Empty
                            ));

                        #endregion

                        //Repaint();

                        Event.current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    //GUIUtility.hotControl = 0;
                    //if (Event.current.button == 0)
                    //{

                    //}

                    //Event.current.Use();
                    break;
            }

            #endregion

            if (m_SelectedObjects != null)
            {
                for (int i = 0; i < m_SelectedObjects?.Length; i++)
                {
                    if (m_SelectedObjects[i] == null) continue;

                    DrawSelectedObject(m_SelectedObjects[i], m_SelectedGameObjects[i]);
                }
            }

            //#region GL Draw All previews

            //if (m_SelectedObject == null)
            //{
            //    Color temp = Color.white;
            //    temp.a = .5f;
            //    Handles.color = temp;

            //    for (int i = 0; i < m_CreatedObjects.Count; i++)
            //    {
            //        MapDataEntityBase.Object obj = GetMapDataObject(m_CreatedObjects[i], out _);

            //        Vector2 pos = HandleUtility.WorldToGUIPoint(obj.m_Translation);
            //        if (!obj.m_Object.IsValid() ||
            //            !EditorSceneUtils.IsDrawable(pos)) continue;

            //        AABB aabb = obj.aabb;
            //        Handles.DrawWireCube(aabb.center, aabb.size);
            //    }
            //}

            //#endregion
        }
        public void Dispose()
        {
            for (int i = 0; i < m_LoadedMapData.Count; i++)
            {
                m_LoadedMapData[i].Dispose();
            }

            m_LoadedMapDataReference.Clear();
            m_LoadedMapData.Clear();

            UnityEngine.Object.DestroyImmediate(Folder.gameObject);
        }

        private void DrawSelectedObject(MapDataEntityBase.Object obj, GameObject proxy)
        {
            const float width = 180;

            EntityBase objData = obj.m_Object.GetObject();
            if (objData == null)
            {
                Handles.BeginGUI();
                Rect tempRect = new Rect(HandleUtility.WorldToGUIPoint(obj.m_Translation), new Vector2(width, 60));
                GUI.BeginGroup(tempRect, "INVALID", EditorStyleUtilities.Box);

                if (GUI.Button(GUILayoutUtility.GetRect(width, 20, GUILayout.ExpandWidth(false)), "Remove"))
                {
                    m_SelectedMapData.Remove(proxy);

                    m_SelectedGameObjects = null;
                    m_SelectedMapData = null;
                    m_SelectedObjects = null;
                }

                GUI.EndGroup();
                Handles.EndGUI();
                return;
            }

            AABB selectAabb = obj.aabb;

            #region Scene GUI Overlays

            Vector3 worldPos = selectAabb.center; worldPos.y = selectAabb.max.y;
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

            if (guiPos.x + width > Screen.width) guiPos.x = Screen.width - width;
            else
            {
                guiPos.x += 50;
            }
            Rect rect = new Rect(guiPos, new Vector2(width, 60));

            Handles.BeginGUI();
            string objName = $"{(objData != null ? $"{objData.Name}" : "None")}";
            GUI.BeginGroup(rect, objName, EditorStyleUtilities.Box);

            #region Proxy Copy

            obj.m_Translation = proxy.transform.position;
            obj.m_Rotation = proxy.transform.rotation;
            obj.m_Scale = proxy.transform.localScale;

            obj.m_Static = proxy.isStatic;

            #endregion

            if (GUI.Button(GUILayoutUtility.GetRect(width, 20, GUILayout.ExpandWidth(false)), "Remove"))
            {
                if (EditorUtility.DisplayDialog($"Remove ({objName})", "Are you sure?", "Remove", "Cancel"))
                {
                    m_SelectedMapData.Remove(proxy);
                    
                    m_SelectedGameObjects = null;
                    m_SelectedMapData = null;
                    m_SelectedObjects = null;

                    //Repaint();
                }
            }
            GUI.EndGroup();
            Handles.EndGUI();

            #endregion

            if (m_SelectedObjects == null) return;

            Handles.color = Color.red;
            Handles.DrawWireCube(selectAabb.center, selectAabb.size);
        }

        private static void DrawMapDataSelector(IFixedReference current, Action<Reference<MapDataEntityBase>> setter)
        {
            ReferenceDrawer.DrawReferenceSelector("Map data: ", (hash) =>
            {
                if (hash.Equals(Hash.Empty))
                {
                    setter.Invoke(Reference<MapDataEntityBase>.Empty);
                    return;
                }

                Reference<MapDataEntityBase> reference = new Reference<MapDataEntityBase>(hash);
                setter.Invoke(reference);
            }, current, TypeHelper.TypeOf<MapDataEntityBase>.Type);
        }

        public sealed class MapData : IEquatable<MapData>, IDisposable
        {
            const string c_EditorOnly = "EditorOnly";

            private readonly Transform m_Folder = null;
            private readonly MapDataEntityBase m_MapData;
            private readonly List<MapDataEntityBase.Object> m_MapDataObjects;

            private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();
            private readonly Dictionary<GameObject, MapDataEntityBase.Object> m_Dictionary = new Dictionary<GameObject, MapDataEntityBase.Object>();

            public bool IsDirty { get; private set; } = false;
            public IReadOnlyList<GameObject> CreatedObjects => m_CreatedObjects;
            public MapDataEntityBase Entity => m_MapData;

            public MapDataEntityBase.Object this[GameObject i]
            {
                get
                {
                    if (!m_Dictionary.TryGetValue(i, out MapDataEntityBase.Object obj)) return null;
                    return obj;
                }
            }
            public GameObject this[MapDataEntityBase.Object i]
            {
                get
                {
                    if (!m_MapDataObjects.Contains(i)) return null;

                    int idx = m_MapDataObjects.IndexOf(i);
                    return m_CreatedObjects[idx];
                }
            }

            public MapData(Transform parent, Reference<MapDataEntityBase> reference)
            {
                m_MapData = reference.GetObject();

                m_Folder = new GameObject(m_MapData.Name).transform;
                m_Folder.SetParent(parent);

                m_MapDataObjects = m_MapData.m_Objects.ToList();
                for (int i = 0; i < m_MapDataObjects.Count; i++)
                {
                    GameObject obj = InstantiateObject(m_Folder, m_MapDataObjects[i]);

                    m_CreatedObjects.Add(obj);
                    m_Dictionary.Add(obj, m_MapDataObjects[i]);
                }
            }

            #region Add & Remove

            public GameObject Add(MapDataEntityBase.Object target)
            {
                GameObject obj = InstantiateObject(m_Folder, target);

                m_MapDataObjects.Add(target);

                m_CreatedObjects.Add(obj);
                m_Dictionary.Add(obj, target);

                m_MapData.m_Objects = m_MapDataObjects.ToArray();
                IsDirty = true;

                return obj;
            }
            public void Remove(MapDataEntityBase.Object target)
            {
                GameObject obj = this[target];
                if (obj == null) return;

                Remove(obj);
            }
            public void Remove(GameObject target)
            {
                if (!m_Dictionary.TryGetValue(target, out MapDataEntityBase.Object data))
                {
                    return;
                }

                m_MapDataObjects.Remove(data);

                m_CreatedObjects.Remove(target);
                m_Dictionary.Remove(target);

                UnityEngine.Object.DestroyImmediate(target);
                m_MapData.m_Objects = m_MapDataObjects.ToArray();
                IsDirty = true;
            }

            #endregion

            public void SetDirty()
            {
                IsDirty = true;
            }

            private static GameObject InstantiateObject(Transform parent, MapDataEntityBase.Object target)
            {
                GameObject obj;
                if (!target.m_Object.IsValid() || 
                    target.m_Object.GetObject().Prefab.IsNone() ||
                    !target.m_Object.GetObject().Prefab.IsValid())
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.SetParent(parent);
                }
                else
                {
                    var temp = target.m_Object.GetObject().Prefab.GetEditorAsset();
                    if (!(temp is GameObject gameObj))
                    {
                        obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        obj.transform.SetParent(parent);
                    }
                    else
                    {
                        obj = (GameObject)PrefabUtility.InstantiatePrefab(gameObj, parent);
                    }
                    //
                }

                //obj.tag = c_EditorOnly;
                //obj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                Transform tr = obj.transform;

                tr.position = target.m_Translation;
                tr.rotation = target.m_Rotation;
                tr.localScale = target.m_Scale;

                //obj.isStatic = target.m_Static;
                if (target.m_Static)
                {
                    SetStaticRecursive(obj);
                }

                return obj;

                void SetStaticRecursive(GameObject obj)
                {
                    for (int i = 0; i < obj.transform.childCount; i++)
                    {
                        if (obj.transform.GetChild(i).childCount > 0)
                        {
                            SetStaticRecursive(obj.transform.GetChild(i).gameObject);
                        }

                        obj.transform.GetChild(i).gameObject.isStatic = true;
                    }

                    obj.isStatic = true;
                }
            }

            public void Dispose()
            {
                if (IsDirty)
                {
                    EntityDataList.Instance.SaveData(m_MapData);
                }
                
                UnityEngine.Object.DestroyImmediate(m_Folder.gameObject);
            }

            public bool Equals(MapData other) => m_MapData.Equals(other.m_MapData);
        }
    }
}
