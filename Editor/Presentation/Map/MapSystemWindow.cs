using Syadeu;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using SyadeuEditor.Tree;
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

        private static string[] s_ToolbarNames = new string[] { "MapData", "SceneData" };
        private int m_SelectedToolbar = 0;

        protected override void OnEnable()
        {
            m_PreviewFolder = new GameObject("Preview").transform;
            m_PreviewFolder.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            m_PreviewFolder.gameObject.tag = c_EditorOnly;

            base.OnEnable();
        }
        protected override void OnDisable()
        {
            foreach (var item in m_LoadedMapData)
            {
                if (item == null) continue;

                item.Dispose();
            }

            DestroyImmediate(m_PreviewFolder.gameObject);
            Tools.hidden = false;

            base.OnDisable();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            //EditorUtils.StringHeader("Map System", 20, true);
            GUILayout.Space(5);
            EditorUtils.Line();

            EditorGUI.BeginChangeCheck();
            m_SelectedToolbar = GUILayout.Toolbar(m_SelectedToolbar, s_ToolbarNames);
            if (EditorGUI.EndChangeCheck())
            {
                //ResetAll();

                SceneView.lastActiveSceneView.Repaint();
                Tools.hidden = false;
            }

            EditorGUI.BeginChangeCheck();
            m_EnableEdit = EditorGUILayout.ToggleLeft("Enable Edit", m_EnableEdit);
            if (EditorGUI.EndChangeCheck())
            {
                if (!m_EnableEdit) Tools.hidden = false;
            }
            if (GUILayout.Button("Show Tools")) Tools.hidden = false;
            EditorGUILayout.Space();

            switch (m_SelectedToolbar)
            {
                case 0:
                    MapDataGUI();
                    break;
                case 1:
                    //SceneDataGUI();
                    break;
                default:
                    break;
            }
        }
        protected override void OnSceneGUI(SceneView obj)
        {
            switch (m_SelectedToolbar)
            {
                case 0:
                    MapDataSceneGUI(obj);
                    break;
                case 1:
                    //SceneDataSceneGUI(obj);
                    break;
                default:
                    break;
            }
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
            public readonly ManagedGrid m_SceneDataGrid;
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
                m_SceneDataGrid = new ManagedGrid(m_SceneDataGridAtt.Center, m_SceneDataGridAtt.Size, m_SceneDataGridAtt.CellSize);

                ReloadLayers();
            }
            public void Dispose()
            {
                m_SceneDataGrid?.Dispose();
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
                EditorUtils.StringRich("GridMapAttribute Extension", 13);

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

                m_EditLayer = GUILayout.Toggle(m_EditLayer, "E", EditorUtils.MiniButton, GUILayout.Width(20));
                if (m_EditLayer)
                {

                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                #endregion

                EditorUtils.Line();

                // Layer Info
                #region Layer Info
                EditorGUI.indentLevel += 1;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorUtils.StringRich($"Layer: {m_GridLayerNames[m_SelectedGridLayer]}", 13);
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

                EditorUtils.Line();
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

        #region Map Data

        private List<Reference<MapDataEntityBase>> m_SelectedMapData = new List<Reference<MapDataEntityBase>>();
        private List<MapData> m_LoadedMapData = new List<MapData>();
        private MapData m_EditingMapData = null;

        private bool m_EnableEdit = true;

        // Mouse Selection
        private MapObject m_SelectedMapObject = null;
        private float3 m_SelectedObjectRotation;
        private bool m_SelectedGameObjectOpen = false;
        //

        private void Select(GameObject obj)
        {
            for (int i = 0; i < m_LoadedMapData.Count; i++)
            {
                m_SelectedMapObject = m_LoadedMapData[i].GetData(obj);
                if (m_SelectedMapObject != null) break;
            }
            Repaint();
        }
        private void DeSelect()
        {
            if (m_SelectedMapObject == null) return;

            EntityDataList.Instance.SaveData(m_SelectedMapObject.Parent.MapDataEntityBase);
            m_SelectedMapObject = null;
            Repaint();
        }

        private void MapDataGUI()
        {
            #region Scene data selector
            using (new EditorUtils.BoxBlock(Color.gray))
            {
                ReflectionHelperEditor.DrawReferenceSelector("Scene data: ", (hash) =>
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
                    using (new EditorUtils.BoxBlock(Color.black))
                    {
                        m_GridMap.OnGUI();
                    }
                }

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
            }
            #endregion

            #region Map data selector
            using (new EditorUtils.BoxBlock(Color.gray))
            {
                for (int i = 0; i < m_SelectedMapData.Count; i++)
                {
                    int index = i;

                    EditorGUILayout.BeginHorizontal();
                    ReflectionHelperEditor.DrawReferenceSelector("Map data: ", (hash) =>
                    {
                        if (hash.Equals(Hash.Empty))
                        {
                            if (m_LoadedMapData[index] != null)
                            {
                                m_LoadedMapData[index].Dispose();
                            }

                            m_SelectedMapObject = null;
                            m_SelectedMapData.RemoveAt(index);
                            m_LoadedMapData.RemoveAt(index);
                            return;
                        }

                        m_SelectedMapData[index] = new Reference<MapDataEntityBase>(hash);
                        MapDataEntityBase mapData = m_SelectedMapData[index].GetObject();

                        if (m_LoadedMapData[index] == null)
                        {
                            m_LoadedMapData[index] = new MapData(m_PreviewFolder, mapData);
                        }
                        else if (!m_LoadedMapData[index].MapDataEntityBase.Idx.Equals(mapData))
                        {
                            m_LoadedMapData[index].Dispose();
                            m_LoadedMapData[index] = new MapData(m_PreviewFolder, mapData);
                        }

                        SceneView.lastActiveSceneView.Repaint();

                        if (m_EnableEdit) Tools.hidden = true;

                    }, m_SelectedMapData[index], TypeHelper.TypeOf<MapDataEntityBase>.Type);

                    bool selected = m_EditingMapData == null ? false : m_EditingMapData.Equals(m_LoadedMapData[i]);
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.BeginDisabledGroup(m_LoadedMapData[i] == null);
                    selected = GUILayout.Toggle(selected, "E", EditorUtils.MiniButton, GUILayout.Width(20));
                    EditorGUI.EndDisabledGroup();
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (selected) m_EditingMapData = m_LoadedMapData[i];
                        else m_EditingMapData = null;
                    }

                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        if (m_LoadedMapData[i] != null)
                        {
                            m_LoadedMapData[i].Dispose();
                        }

                        m_SelectedMapObject = null;
                        m_SelectedMapData.RemoveAt(i);
                        m_LoadedMapData.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                ReflectionHelperEditor.DrawReferenceSelector("Map data: ", (hash) =>
                {
                    var newRef = new Reference<MapDataEntityBase>(hash);
                    if (m_SelectedMapData.Contains(newRef))
                    {
                        "cannot load that already loaded".ToLog();
                        return;
                    }

                    MapDataEntityBase mapData = newRef.GetObject();

                    m_SelectedMapData.Add(newRef);
                    m_LoadedMapData.Add(new MapData(m_PreviewFolder, mapData));

                    SceneView.lastActiveSceneView.Repaint();
                    Tools.hidden = true;

                }, Reference<MapDataEntityBase>.Empty, TypeHelper.TypeOf<MapDataEntityBase>.Type);

                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Toggle(false, "E", EditorUtils.MiniButton, GUILayout.Width(20));
                GUILayout.Button("-", GUILayout.Width(20));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
            }
            #endregion

            EditorUtils.Line();

            if (Application.isPlaying)
            {
                EditorUtils.StringRich(c_EditInPlayingWarning, 13, true);
                return;
            }

            if (m_SelectedMapObject != null)
            {
                EntityBase entity = m_SelectedMapObject.Data.m_Object.GetObject();
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    EditorUtils.StringRich(entity.Name, 15);

                    #region Position
                    EditorGUI.BeginChangeCheck();
                    m_SelectedMapObject.Data.m_Translation
                        = EditorGUILayout.Vector3Field("Position", m_SelectedMapObject.Data.m_Translation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SelectedMapObject.GameObject.transform.position
                            = m_SelectedMapObject.Data.m_Translation;

                        EntityDataList.Instance.SaveData(entity);
                        SceneView.lastActiveSceneView.Repaint();
                    }
                    #endregion

                    #region Rotation
                    EditorGUI.BeginChangeCheck();
                    m_SelectedObjectRotation
                        = EditorGUILayout.Vector3Field("Rotation", m_SelectedMapObject.Data.m_Rotation.Euler() * Mathf.Rad2Deg);
                    if (EditorGUI.EndChangeCheck())
                    {
                        quaternion rot = Quaternion.Euler(m_SelectedObjectRotation);
                        m_SelectedMapObject.Data.m_Rotation = rot;
                        m_SelectedMapObject.GameObject.transform.rotation = rot;

                        EntityDataList.Instance.SaveData(entity);
                        SceneView.lastActiveSceneView.Repaint();
                    }
                    #endregion

                    #region Scale

                    EditorGUI.BeginChangeCheck();

                    m_SelectedMapObject.Data.m_Scale = EditorGUILayout.Vector3Field("Scale",
                        m_SelectedMapObject.Data.m_Scale);

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SelectedMapObject.GameObject.transform.localScale = m_SelectedMapObject.Data.m_Scale;
                        EntityDataList.Instance.SaveData(entity);
                        SceneView.lastActiveSceneView.Repaint();
                    }

                    #endregion

                    EditorGUILayout.Space();
                    EditorUtils.StringRich("AABB", 13, true);
                    {
                        if (GUILayout.Button("Auto"))
                        {
                            GameObject temp = (GameObject)entity.Prefab.GetEditorAsset();
                            if (temp != null)
                            {
                                Transform tr = temp.transform;

                                AABB aabb = new AABB(float3.zero, float3.zero);
                                foreach (var item in tr.GetComponentsInChildren<Renderer>())
                                {
                                    aabb.Encapsulate(item.bounds);
                                }
                                entity.Center = aabb.center - ((float3)tr.position);
                                entity.Size = aabb.size;

                                EntityDataList.Instance.SaveData(entity);
                                SceneView.lastActiveSceneView.Repaint();
                            }
                            else
                            {
                                entity.Center = 0;
                                entity.Size = 1;
                            }
                        }
                        entity.Center = EditorGUILayout.Vector3Field("Center", entity.Center);
                        entity.Size = EditorGUILayout.Vector3Field("Size", entity.Size);
                    }

                    var gridSizeAtt = entity.GetAttribute<GridSizeAttribute>();
                    if (gridSizeAtt != null)
                    {
                        ReflectionHelperEditor.GetDrawer(gridSizeAtt).OnGUI();
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Save"))
                    {
                        EntityDataList.Instance.SaveData(m_SelectedMapObject.Parent.MapDataEntityBase);
                        EntityDataList.Instance.SaveData(entity);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        Color whiteColor = Color.white;
        private void MapDataSceneGUI(SceneView obj)
        {
            m_GridMap?.OnSceneGUI(obj);

            if (m_LoadedMapData.Count == 0 || !m_EnableEdit) return;
            int mouseControlID = GUIUtility.GetControlID(FocusType.Passive);
            int keyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            Selection.activeObject = null;

            #region Scene Mouse Event

            switch (Event.current.GetTypeForControl(mouseControlID))
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        if (m_SelectedMapObject == null)
                        {
                            GameObject tempObj = HandleUtility.PickGameObject(Event.current.mousePosition, true);
                            Select(tempObj);
                        }
                    }
                    else if (Event.current.button == 2)
                    {
                        if (m_EditingMapData == null)
                        {
                            "no editting map data".ToLog();
                            return;
                        }

                        GUIUtility.hotControl = mouseControlID;

                        DeSelect();

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
                                Reference<EntityBase> refobj = new Reference<EntityBase>(hash);

                                m_SelectedMapObject = m_EditingMapData.Add(refobj, m_PreviewFolder, pos);
                                Repaint();
                            },
                            getter: (other) => other.Hash,
                            noneValue: Hash.Empty
                            ));
                        #endregion

                        Repaint();

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
                default:
                    break;
            }
            #endregion

            #region Object Selection Draw
            
            if (m_SelectedMapObject != null)
            {
                const float width = 180;

                EntityBase objData = m_SelectedMapObject.Data.m_Object.GetObject();
                GameObject previewObj = m_SelectedMapObject.GameObject;
                AABB selectAabb = m_SelectedMapObject.Data.aabb;

                #region Scene GUI Overlays

                Vector3 worldPos = selectAabb.center; worldPos.y = selectAabb.max.y;
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

                if (guiPos.x + width > Screen.width) guiPos.x = Screen.width - width;
                else
                {
                    guiPos.x += 50;
                }
                Rect rect = new Rect(guiPos, new Vector2(width, m_SelectedGameObjectOpen ? 105 : 60));

                Handles.BeginGUI();
                string objName = $"{(objData != null ? $"{objData.Name}" : "None")}";
                GUI.BeginGroup(rect, objName, EditorUtils.Box);

                #region TR

                m_SelectedGameObjectOpen = EditorGUILayout.Foldout(m_SelectedGameObjectOpen, "Transform", true);

                if (m_SelectedGameObjectOpen)
                {
                    EditorGUI.BeginChangeCheck();
                    m_SelectedMapObject.Data.m_Translation = EditorGUILayout.Vector3Field(string.Empty, m_SelectedMapObject.Data.m_Translation, GUILayout.Width(width - 5), GUILayout.ExpandWidth(false));
                    //m_SelectedGameObject.m_Rotation = EditorGUILayout.Vector3Field(string.Empty, m_SelectedGameObject.eulerAngles, GUILayout.Width(width - 5), GUILayout.ExpandWidth(false));
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SelectedMapObject.GameObject.transform.position = m_SelectedMapObject.Data.m_Translation;
                    }
                }

                #endregion

                if (GUI.Button(GUILayoutUtility.GetRect(width, 20, GUILayout.ExpandWidth(false)), "Remove"))
                {
                    if (EditorUtility.DisplayDialog($"Remove ({objName})", "Are you sure?", "Remove", "Cancel"))
                    {
                        m_SelectedMapObject.Destroy();
                        m_SelectedMapObject = null;

                        Repaint();
                        goto DrawAllPreviewSection;
                    }
                }
                GUI.EndGroup();
                Handles.EndGUI();

                #endregion

                #region Tools

                EditorGUI.BeginChangeCheck();
                switch (Tools.current)
                {
                    case Tool.View:
                        break;
                    case Tool.Move:
                        DrawMoveTool(m_SelectedMapObject);
                        break;
                    case Tool.Rotate:
                        DrawRotationTool(m_SelectedMapObject);
                        break;
                    case Tool.Scale:
                        DrawScaleTool(m_SelectedMapObject);
                        break;
                    case Tool.Rect:
                        break;
                    case Tool.Transform:
                        break;
                    default:
                        break;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Repaint();
                }

                #endregion

                Handles.color = Color.red;
                Handles.DrawWireCube(selectAabb.center, selectAabb.size);

                if (Event.current.isKey)
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        DeSelect();
                    }
                }
            }

            #endregion

            DrawAllPreviewSection:

            #region GL Draw All previews

            if (m_SelectedMapObject == null)
            {
                whiteColor.a = .5f;
                Handles.color = whiteColor;

                foreach (var item in m_LoadedMapData)
                {
                    if (item == null) continue;

                    for (int i = 0; i < item.MapDataEntityBase.m_Objects?.Length; i++)
                    {
                        Vector2 pos = HandleUtility.WorldToGUIPoint(item.MapDataEntityBase.m_Objects[i].m_Translation);
                        if (!item.MapDataEntityBase.m_Objects[i].m_Object.IsValid() ||
                            !EditorSceneUtils.IsDrawable(pos)) continue;

                        AABB aabb = item.MapDataEntityBase.m_Objects[i].aabb;
                        Handles.DrawWireCube(aabb.center, aabb.size);
                    }
                }
            }
            
            #endregion
        }

        #region Tool
        private quaternion invalid = new quaternion(0, 0, 0, 0);
        private void DrawMoveTool(MapObject obj)
        {
            if (obj.Rotation.Equals(invalid)) obj.Rotation = quaternion.identity;

            obj.Position = Handles.PositionHandle(obj.Position, obj.Rotation);
        }
        private void DrawRotationTool(MapObject obj)
        {
            if (obj.Rotation.Equals(invalid)) obj.Rotation = quaternion.identity;

            obj.Rotation = Handles.RotationHandle(obj.Rotation, obj.Position);
        }
        private void DrawScaleTool(MapObject obj)
        {
            if (obj.Rotation.Equals(invalid)) obj.Rotation = quaternion.identity;

            obj.Scale = Handles.ScaleHandle(obj.Scale, obj.Position, obj.Rotation, HandleUtility.GetHandleSize(obj.Position));
        }
        #endregion

        #endregion
    }
}
