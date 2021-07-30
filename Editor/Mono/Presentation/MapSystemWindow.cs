using Syadeu;
using Syadeu.Database;
using Syadeu.Internal;
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
                ResetAll();

                SceneView.lastActiveSceneView.Repaint();
                Tools.hidden = false;
            }

            switch (m_SelectedToolbar)
            {
                case 0:
                    MapDataGUI();
                    break;
                case 1:
                    SceneDataGUI();
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
                    SceneDataSceneGUI(obj);
                    break;
                default:
                    break;
            }
        }

        #region Common
        private Transform m_PreviewFolder;
        private readonly Dictionary<MapDataEntity.Object, GameObject> m_PreviewObjects = new Dictionary<MapDataEntity.Object, GameObject>();
        const string c_EditInPlayingWarning = "Cannot edit data while playing";
        private void SaveNCloseButton()
        {
            if (GUILayout.Button("Revert"))
            {
                EntityDataList.Instance.LoadData();

                ResetAll();
                SceneView.lastActiveSceneView.Repaint();
                Tools.hidden = false;
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                EntityDataList.Instance.SaveData();
            }
            if (GUILayout.Button("Close"))
            {
                ResetAll();

                SceneView.lastActiveSceneView.Repaint();
                Tools.hidden = false;
            }
            EditorGUILayout.EndHorizontal();
        }
        private void ResetAll()
        {
            DeselectGameObject();
            ResetPreviewFolder();

            m_MapData = new Reference<MapDataEntity>(Hash.Empty);
            m_MapDataTarget = null;

            m_SceneData = new Reference<SceneDataEntity>(Hash.Empty);
            m_SceneDataTarget = null;
            m_SceneDataTargetMapDataList = null;
            m_AttributeListDrawer = null;

            // GridMapAttribute
            m_GridMap?.Dispose();
            m_GridMap = null;
        }
        private void ResetPreviewFolder()
        {
            if (m_PreviewFolder != null) DestroyImmediate(m_PreviewFolder.gameObject);
            m_PreviewFolder = new GameObject("Preview").transform;
            m_PreviewFolder.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            m_PreviewFolder.gameObject.tag = c_EditorOnly;

            m_PreviewObjects.Clear();
        }
        private void CreatePreviewObjects(MapDataEntity mapData)
        {
            if (mapData.m_Objects == null) mapData.m_Objects = Array.Empty<MapDataEntity.Object>();
            for (int i = 0; i < mapData.m_Objects.Length; i++)
            {
                if (!mapData.m_Objects[i].m_Object.IsValid()) continue;

                CreatePreviewObject(mapData.m_Objects[i]);
                //PrefabReference prefab = mapData.m_Objects[i].m_Object.GetObject().Prefab;
                //if (prefab.IsValid())
                //{
                //    var temp = prefab.GetObjectSetting().m_RefPrefab.editorAsset;

                //    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(temp, m_PreviewFolder);
                //    obj.tag = c_EditorOnly;
                //    obj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                //    Transform tr = obj.transform;
                //    tr.position = mapData.m_Objects[i].m_Translation;
                //    tr.rotation = mapData.m_Objects[i].m_Rotation;
                //    tr.localScale = mapData.m_Objects[i].m_Scale;

                //    AABB aabb = new AABB(mapData.m_Objects[i].m_Translation, float3.zero);
                //    foreach (var item in obj.GetComponentsInChildren<Renderer>())
                //    {
                //        aabb.Encapsulate(item.bounds);
                //    }
                //    mapData.m_Objects[i].m_AABBCenter = aabb.center - mapData.m_Objects[i].m_Translation;
                //    mapData.m_Objects[i].m_AABBSize = aabb.size;

                //    m_PreviewObjects.Add(mapData.m_Objects[i], obj);
                //}
            }
        }
        private GameObject CreatePreviewObject(MapDataEntity.Object mapDataObj, bool isFirst = false)
        {
            if (!mapDataObj.m_Object.IsValid()) return null;

            PrefabReference prefab = mapDataObj.m_Object.GetObject().Prefab;
            if (prefab.IsValid())
            {
                GameObject temp = (GameObject)prefab.GetObjectSetting().m_RefPrefab.editorAsset;

                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(temp, m_PreviewFolder);
                obj.tag = c_EditorOnly;
                obj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                Transform tr = obj.transform;

                if (isFirst)
                {
                    mapDataObj.m_Rotation = tr.rotation;
                    mapDataObj.m_Scale = tr.localScale;

                    AABB aabb = new AABB(tr.position, float3.zero);
                    foreach (var item in obj.GetComponentsInChildren<Renderer>())
                    {
                        aabb.Encapsulate(item.bounds);
                    }
                    mapDataObj.m_AABBCenter = aabb.center - ((float3)tr.position);
                    mapDataObj.m_AABBSize = aabb.size;
                }

                tr.position = mapDataObj.m_Translation;
                tr.rotation = mapDataObj.m_Rotation;
                tr.localScale = mapDataObj.m_Scale;

                m_PreviewObjects.Add(mapDataObj, obj);
                return obj;
            }

            return null;
        }

        private MapDataEntity.Object m_SelectedGameObject;
        private bool m_SelectedGameObjectOpen = false;
        private void SelectGameObject(GameObject obj)
        {
            var iter = m_PreviewObjects.Where((other) => other.Value.Equals(obj));
            if (iter.Any())
            {
                var target = iter.First();
                if (m_SelectedGameObject != null)
                {
                    if (target.Value.Equals(m_SelectedGameObject)) return;
                    else
                    {
                        DeselectGameObject();
                    }
                }

                m_SelectedGameObject = target.Key;
                m_PreviewObjects[m_SelectedGameObject].SetActive(false);
                Repaint();
            }
        }
        private void DeselectGameObject()
        {
            if (m_SelectedGameObject != null)
            {
                m_PreviewObjects[m_SelectedGameObject].SetActive(true);
                m_SelectedGameObject = null;

                Repaint();
            }
        }

        #endregion

        #region Scene Data

        private Reference<SceneDataEntity> m_SceneData;
        private SceneDataEntity m_SceneDataTarget;
        Reference<MapDataEntity>[] m_SceneDataTargetMapDataList;
        private ReflectionHelperEditor.AttributeListDrawer m_AttributeListDrawer;

        // GridMapAttribute
        private sealed class GridMapExtension : IDisposable
        {
            public readonly GridMapAttribute m_SceneDataGridAtt;
            public readonly ManagedGrid m_SceneDataGrid;
            public string[] m_GridLayerNames;

            public int m_SelectedGridLayer = 0;
            public GridMapAttribute.LayerInfo m_CurrentLayer = null;

            public bool m_EditLayer = false;

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

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                m_SelectedGridLayer = EditorGUILayout.Popup("Grid Layer: ", m_SelectedGridLayer, m_GridLayerNames);
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

                    m_SelectedGridLayer = m_SceneDataGridAtt.m_Layers.Length - 1;
                }
                EditorGUI.BeginDisabledGroup(m_SelectedGridLayer == 0);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    var temp = m_SceneDataGridAtt.m_Layers.ToList();
                    temp.RemoveAt(temp.Count - 1);
                    m_SceneDataGridAtt.m_Layers = temp.ToArray();

                    ReloadLayers();
                }

                m_EditLayer = GUILayout.Toggle(m_EditLayer, "E", EditorUtils.MiniButton, GUILayout.Width(20));
                if (m_EditLayer)
                {
                    
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
            }
            bool m_AddDrag = false;
            public void OnSceneGUI(SceneView obj)
            {
                if (m_SceneDataGridAtt == null) return;

                #region Draw Grid & Layers

                m_SceneDataGrid.DrawGL();
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
                                    cellPos = m_SceneDataGrid.GetCellPosition(item.m_Indices[i]),
                                    p1 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf),
                                    p2 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                                    p3 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                                    p4 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf);

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
                                cellPos = m_SceneDataGrid.GetCellPosition(m_CurrentLayer.m_Indices[i]),
                                p1 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf),
                                p2 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                                p3 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                                p4 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf);

                            GL.Vertex(p1);
                            GL.Vertex(p2);
                            GL.Vertex(p3);
                            GL.Vertex(p4);
                        }
                    }

                    GL.End();
                    GL.PopMatrix();
                }
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
                                int idx = m_SceneDataGrid.GetCellIndex(point);
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
                            int idx = m_SceneDataGrid.GetCellIndex(point);
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
        private GridMapExtension m_GridMap;

        private Vector2 m_SceneDataScroll;
        private void SceneDataGUI()
        {
            #region Scene data selector
            using (new EditorUtils.BoxBlock(Color.gray))
            {
                ReflectionHelperEditor.DrawReferenceSelector("Scene data: ", (hash) =>
                {
                    var tempSceneData = new Reference<SceneDataEntity>(hash);

                    if (tempSceneData.IsValid() && !m_SceneData.Equals(tempSceneData))
                    {
                        m_SceneDataTarget = tempSceneData.GetObject();

                        m_GridMap = new GridMapExtension(m_SceneDataTarget.GetAttribute<GridMapAttribute>());

                        m_SceneDataTargetMapDataList = (Reference<MapDataEntity>[])TypeHelper.TypeOf<SceneDataEntity>.Type.GetField("m_MapData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(m_SceneDataTarget);
                        if (m_SceneDataTargetMapDataList != null)
                        {
                            foreach (var item in m_SceneDataTargetMapDataList)
                            {
                                if (!item.IsValid()) continue;

                                MapDataEntity mapData = item.GetObject();
                                CreatePreviewObjects(mapData);
                            }
                            //
                        }

                        m_AttributeListDrawer = ReflectionHelperEditor.GetAttributeDrawer(TypeHelper.TypeOf<SceneDataEntity>.Type, m_SceneDataTarget.Attributes);

                        Tools.hidden = true;
                    }
                    else
                    {
                        ResetAll();

                        Tools.hidden = false;
                    }

                    m_SceneData = tempSceneData;

                }, m_SceneData, TypeHelper.TypeOf<SceneDataEntity>.Type);
            }
            #endregion

            EditorUtils.Line();

            if (Application.isPlaying)
            {
                EditorUtils.StringRich(c_EditInPlayingWarning, 13, true);
                return;
            }

            if (!m_SceneData.IsValid())
            {
                EditorGUILayout.Space();
                EditorUtils.StringRich("Select scene data", 13, true);
                return;
            }

            SaveNCloseButton();
            EditorUtils.Line();

            using (new EditorUtils.BoxBlock(Color.black))
            {
                m_GridMap?.OnGUI();
            }

            m_SceneDataScroll = GUILayout.BeginScrollView(m_SceneDataScroll, false, false);
            if (m_SceneDataTarget != null)
            {
                using (new EditorUtils.BoxBlock(Color.gray))
                {
                    EditorUtils.StringRich("SceneData", 13);

                    EditorGUI.BeginDisabledGroup(true);
                    ReflectionHelperEditor.DrawObject(m_SceneDataTarget, "Name", "Hash", "m_BindScene", "m_SceneIndex");

                    EditorGUI.EndDisabledGroup();

                    EditorUtils.Line();

                    m_AttributeListDrawer.OnGUI();
                }

                EditorUtils.Line();
            }
            GUILayout.EndScrollView();
        }
        private void SceneDataSceneGUI(SceneView obj)
        {
            m_GridMap?.OnSceneGUI(obj);
        }

        #endregion

        #region Map Data

        private Reference<MapDataEntity> m_MapData;
        private MapDataEntity m_MapDataTarget;
        private Vector2 m_MapDataScroll;
        private VerticalTreeView m_MapDataTreeView;

        private void MapDataGUI()
        {
            #region Scene data selector
            using (new EditorUtils.BoxBlock(Color.gray))
            {
                ReflectionHelperEditor.DrawReferenceSelector("Scene data: ", (hash) =>
                {
                    m_SceneData = new Reference<SceneDataEntity>(hash);

                    if (m_SceneData.IsValid())
                    {
                        m_SceneDataTarget = m_SceneData.GetObject();

                        m_GridMap = new GridMapExtension(m_SceneDataTarget.GetAttribute<GridMapAttribute>());
                        SceneView.lastActiveSceneView.Repaint();
                    }
                }, m_SceneData, TypeHelper.TypeOf<SceneDataEntity>.Type);
            }
            #endregion

            #region Map data selector
            using (new EditorUtils.BoxBlock(Color.gray))
            {
                ReflectionHelperEditor.DrawReferenceSelector("Map data: ", (hash) =>
                {
                    m_MapData = new Reference<MapDataEntity>(hash);

                    if (m_MapDataTarget == null)
                    {
                        m_MapDataTarget = m_MapData.GetObject();
                        SetupTreeView(m_MapDataTarget);
                        CreatePreviewObjects(m_MapDataTarget);

                        SceneView.lastActiveSceneView.Repaint();
                    }
                    else if (!m_MapDataTarget.Idx.Equals(m_MapData))
                    {
                        ResetPreviewFolder();

                        m_MapDataTarget = m_MapData.GetObject();
                        SetupTreeView(m_MapDataTarget);
                        CreatePreviewObjects(m_MapDataTarget);

                        SceneView.lastActiveSceneView.Repaint();
                    }

                    Tools.hidden = true;

                }, m_MapData, TypeHelper.TypeOf<MapDataEntity>.Type);
            }
            #endregion

            EditorUtils.Line();

            if (Application.isPlaying)
            {
                EditorUtils.StringRich(c_EditInPlayingWarning, 13, true);
                return;
            }

            if (m_GridMap != null && m_GridMap.m_SceneDataGridAtt != null)
            {
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    m_GridMap.OnGUI();
                }
            }

            if (!m_MapData.IsValid())
            {
                EditorGUILayout.Space();
                EditorUtils.StringRich("Select map data", 13, true);
                return;
            }

            if (m_SelectedGameObject != null)
            {
                var entity = m_SelectedGameObject.m_Object.GetObject();
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    EditorUtils.StringRich(entity.Name, 13);

                    var gridSizeAtt = entity.GetAttribute<GridSizeAttribute>();
                    if (gridSizeAtt != null)
                    {
                        Vector2Int tempGridSize = new Vector2Int(gridSizeAtt.m_GridSize.x, gridSizeAtt.m_GridSize.y);
                        tempGridSize = EditorGUILayout.Vector2IntField("Grid Size: ", tempGridSize);
                        gridSizeAtt.m_GridSize = new int2(tempGridSize.x, tempGridSize.y);
                    }
                }
            }

            using (new EditorUtils.BoxBlock(Color.black))
            {
                SaveNCloseButton();

                int screenWidth = Screen.width;
                m_MapDataScroll = GUILayout.BeginScrollView(m_MapDataScroll, false, false);

                m_MapDataTreeView.OnGUI();

                GUILayout.EndScrollView();
            }
        }
        Color whiteColor = Color.white;
        private void MapDataSceneGUI(SceneView obj)
        {
            m_GridMap?.OnSceneGUI(obj);

            if (m_MapDataTarget == null) return;
            Selection.activeObject = null;

            #region Scene Mouse Event
            int mouseControlID = GUIUtility.GetControlID(FocusType.Passive);
            switch (Event.current.GetTypeForControl(mouseControlID))
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        var tempObj = HandleUtility.PickGameObject(Event.current.mousePosition, true);
                        SelectGameObject(tempObj);
                    }
                    else if (Event.current.button == 2)
                    {
                        DeselectGameObject();

                        GUIUtility.hotControl = mouseControlID;

                        #region Draw Object Creation PopupWindow
                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;
                        Vector3 pos = EditorSceneUtils.GetMouseScreenPos();

                        var list = EntityDataList.Instance.m_Objects.Where((other) => other.Value is EntityBase).Select((other) => (EntityBase)other.Value).ToArray();
                        PopupWindow.Show(rect, SelectorPopup<Hash, EntityBase>.GetWindow(list,
                            (hash) =>
                            {
                                Reference<EntityBase> refobj = new Reference<EntityBase>(hash);
                                var objData = new MapDataEntity.Object()
                                {
                                    m_Object = refobj,
                                    m_Translation = pos
                                };

                                GameObject gameObj = CreatePreviewObject(objData, true);

                                List<MapDataEntity.Object> tempList = m_MapDataTarget.m_Objects.ToList();
                                tempList.Add(objData);
                                m_MapDataTarget.m_Objects = tempList.ToArray();

                                SelectGameObject(gameObj);
                                m_MapDataTreeView.Refresh(m_MapDataTarget.m_Objects);

                                Repaint();
                            },
                            (other) => other.Hash));
                        #endregion

                        Event.current.Use();

                        Repaint();
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

            #region Object Selection Draw
            ObjectSelectionDraw:
            if (m_SelectedGameObject != null)
            {
                const float width = 180;

                EntityBase objData = m_SelectedGameObject.m_Object.GetObject();
                GameObject previewObj = m_PreviewObjects[m_SelectedGameObject];

                #region Scene GUI Overlays
                string name = $"{(objData != null ? $"{objData.Name}" : "None")}";
                AABB selectAabb = m_SelectedGameObject.AABB;
                Vector3 worldPos = selectAabb.center; worldPos.y = selectAabb.max.y;
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);
                if (!EditorSceneUtils.IsDrawable(guiPos)) goto ObjectSelectionDrawTools;

                if (guiPos.x + width > Screen.width) guiPos.x = Screen.width - width;
                else
                {
                    guiPos.x += 50;
                }

                Rect rect = new Rect(guiPos, new Vector2(width, m_SelectedGameObjectOpen ? 100 : 60));

                Handles.DrawWireCube(selectAabb.center, selectAabb.size);

                Handles.BeginGUI();
                GUI.BeginGroup(rect, name, EditorUtils.Box);

                #region TR

                m_SelectedGameObjectOpen = EditorGUILayout.Foldout(m_SelectedGameObjectOpen, "Transform", true);

                if (m_SelectedGameObjectOpen)
                {
                    EditorGUI.BeginChangeCheck();
                    m_SelectedGameObject.m_Translation = EditorGUILayout.Vector3Field(string.Empty, m_SelectedGameObject.m_Translation, GUILayout.Width(width - 5), GUILayout.ExpandWidth(false));
                    m_SelectedGameObject.eulerAngles = EditorGUILayout.Vector3Field(string.Empty, m_SelectedGameObject.eulerAngles, GUILayout.Width(width - 5), GUILayout.ExpandWidth(false));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (m_PreviewObjects[m_SelectedGameObject] != null)
                        {
                            m_PreviewObjects[m_SelectedGameObject].transform.position = m_SelectedGameObject.m_Translation;
                        }
                    }
                }

                #endregion

                if (GUI.Button(GUILayoutUtility.GetRect(width, 20, GUILayout.ExpandWidth(false)), "Remove"))
                {
                    if (EditorUtility.DisplayDialog($"Remove ({name})", "Are you sure?", "Remove", "Cancel"))
                    {
                        var temp = m_MapDataTarget.m_Objects.ToList();
                        temp.Remove(m_SelectedGameObject);
                        m_MapDataTarget.m_Objects = temp.ToArray();

                        m_PreviewObjects.Remove(m_SelectedGameObject);
                        m_SelectedGameObject = null;
                        m_MapDataTreeView.Refresh(m_MapDataTarget.m_Objects);
                    }
                }
                GUI.EndGroup();
                Handles.EndGUI();
                #endregion

                if (m_SelectedGameObject == null) goto ObjectSelectionDraw;
                ObjectSelectionDrawTools:

                #region Tools

                EditorGUI.BeginChangeCheck();
                switch (Tools.current)
                {
                    case Tool.View:
                        break;
                    case Tool.Move:
                        DrawMoveTool(m_SelectedGameObject);
                        break;
                    case Tool.Rotate:
                        DrawRotationTool(m_SelectedGameObject);
                        break;
                    case Tool.Scale:
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

                #region Draw Mesh with GL
                GridExtensions.DefaultMaterial.SetPass(0);
                GL.PushMatrix();
                GL.Begin(GL.TRIANGLES);
                {
                    whiteColor.a = .35f;
                    GL.Color(whiteColor);

                    Vector3 objPos = previewObj.transform.position;

                    foreach (var item in previewObj.GetComponentsInChildren<MeshFilter>())
                    {
                        Mesh mesh = item.sharedMesh;
                        for (int a = 0; a < mesh.triangles.Length; a += 3)
                        {
                            GL.Vertex(objPos + mesh.vertices[mesh.triangles[a]]);
                            GL.Vertex(objPos + mesh.vertices[mesh.triangles[a + 1]]);
                            GL.Vertex(objPos + mesh.vertices[mesh.triangles[a + 2]]);
                        }
                    }
                }
                GL.End();
                GL.PopMatrix();
                #endregion
            }

            #endregion

            //GLDrawAllPreviews:

            #region GL Draw All previews
            GridExtensions.DefaultMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            {
                whiteColor.a = .25f;
                GL.Color(whiteColor);

                for (int i = 0; i < m_MapDataTarget.m_Objects?.Length; i++)
                {
                    if (m_SelectedGameObject != null && m_SelectedGameObject.Equals(m_MapDataTarget.m_Objects[i])) continue;

                    Vector2 pos = HandleUtility.WorldToGUIPoint(m_MapDataTarget.m_Objects[i].m_Translation);
                    if (!EditorSceneUtils.IsDrawable(pos)) continue;

                    EntityBase objData = m_MapDataTarget.m_Objects[i].m_Object.GetObject();
                    GameObject previewObj = m_PreviewObjects[m_MapDataTarget.m_Objects[i]];

                    Vector3 objPos = previewObj.transform.position;

                    foreach (var item in previewObj.GetComponentsInChildren<MeshFilter>())
                    {
                        Mesh mesh = item.sharedMesh;
                        for (int a = 0; a < mesh.triangles.Length; a += 3)
                        {
                            GL.Vertex(objPos + mesh.vertices[mesh.triangles[a]]);
                            GL.Vertex(objPos + mesh.vertices[mesh.triangles[a + 1]]);
                            GL.Vertex(objPos + mesh.vertices[mesh.triangles[a + 2]]);
                        }
                    }
                }
            }
            GL.End();
            GL.PopMatrix();
            for (int i = 0; i < m_MapDataTarget.m_Objects?.Length; i++)
            {
                if (m_SelectedGameObject != null && m_SelectedGameObject.Equals(m_MapDataTarget.m_Objects[i])) continue;
                Vector2 pos = HandleUtility.WorldToGUIPoint(m_MapDataTarget.m_Objects[i].m_Translation);
                if (!EditorSceneUtils.IsDrawable(pos)) continue;

                AABB aabb = m_MapDataTarget.m_Objects[i].AABB;
                Handles.DrawWireCube(aabb.center, aabb.size);
            }
            #endregion
        }

        #region TreeView
        private void SetupTreeView(MapDataEntity data)
        {
            if (m_MapDataTreeView == null) m_MapDataTreeView = new VerticalTreeView(null, null);

            if (data.m_Objects == null) data.m_Objects = Array.Empty<MapDataEntity.Object>();

            m_MapDataTreeView
                .SetupElements(data.m_Objects, (other) =>
                {
                    MapDataEntity.Object objData = (MapDataEntity.Object)other;
                    return new TreeObjectElement(m_MapDataTreeView, objData, m_PreviewFolder, m_PreviewObjects);
                })
                .MakeAddButton(() =>
                {
                    List<MapDataEntity.Object> temp = data.m_Objects.ToList();

                    var objData = new MapDataEntity.Object();

                    Camera sceneCam = SceneView.lastActiveSceneView.camera;
                    Vector3 pos = sceneCam.transform.position + (sceneCam.transform.forward * 10f);
                    objData.m_Translation = pos;

                    temp.Add(objData);
                    data.m_Objects = temp.ToArray();

                    return data.m_Objects;
                })
                .MakeRemoveButton((other) =>
                {
                    TreeObjectElement element = (TreeObjectElement)other;
                    MapDataEntity.Object target = (MapDataEntity.Object)element.TargetObject;

                    List<MapDataEntity.Object> temp = data.m_Objects.ToList();
                    temp.Remove(target);

                    if (m_PreviewObjects.TryGetValue(target, out var tempObj) && tempObj != null)
                    {
                        DestroyImmediate(m_PreviewObjects[target]);
                        if (m_SelectedGameObject.Equals(target))
                        {
                            DeselectGameObject();
                        }
                        m_PreviewObjects.Remove(target);
                    }

                    data.m_Objects = temp.ToArray();

                    return data.m_Objects;
                })
                ;
        }
        private sealed class TreeObjectElement : VerticalTreeElement<MapDataEntity.Object>
        {
            public override string Name
            {
                get
                {
                    EntityBase temp = Target.m_Object.GetObject();
                    if (temp == null) return "None";
                    else return $"{temp.Name}";
                }
            }

            readonly Transform m_Folder;
            readonly Dictionary<MapDataEntity.Object, GameObject> m_List;

            public TreeObjectElement(VerticalTreeView treeView, MapDataEntity.Object target, Transform folder, Dictionary<MapDataEntity.Object, GameObject> list) : base(treeView, target)
            {
                m_Folder = folder;
                m_List = list;
            }
            public override void OnGUI()
            {
                using (new EditorUtils.BoxBlock(Color.black, GUILayout.ExpandWidth(true)))
                {
                    ReflectionHelperEditor.DrawReferenceSelector("Object",
                        (hash) =>
                        {
                            var target = new Reference<EntityBase>(hash);
                            if (!Target.m_Object.Equals(hash))
                            {
                                if (m_List.TryGetValue(Target, out GameObject gameObj))
                                {
                                    DestroyImmediate(gameObj);
                                }

                                //if (m_PreviewObject != null) DestroyImmediate(m_PreviewObject);

                                if (!hash.Equals(Hash.Empty))
                                {
                                    GameObject temp = (GameObject)target.GetObject().Prefab.GetObjectSetting().m_RefPrefab.editorAsset;

                                    gameObj = (GameObject)PrefabUtility.InstantiatePrefab(temp, m_Folder);
                                    gameObj.tag = c_EditorOnly;
                                    gameObj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                                    Target.m_Rotation = temp.transform.rotation;
                                    Target.m_Scale = temp.transform.localScale;

                                    m_List[Target] = gameObj;
                                }

                                UpdatePreviewObject();
                            }

                            Target.m_Object = target;

                        }, Target.m_Object, TypeHelper.TypeOf<EntityBase>.Type);

                    EditorGUI.BeginChangeCheck();
                    ReflectionHelperEditor.DrawObject(Target, "m_Object");
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdatePreviewObject();
                    }
                }
            }
            public void UpdatePreviewObject()
            {
                if (m_List[Target] == null) return;

                Transform tr = m_List[Target].transform;
                tr.position = Target.m_Translation;
                tr.rotation = Target.m_Rotation;
                tr.localScale = Target.m_Scale;
            }
        }
        #endregion

        #region Tool
        private void DrawMoveTool(MapDataEntity.Object obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.m_Translation = Handles.PositionHandle(obj.m_Translation, obj.m_Rotation);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_PreviewObjects[obj] != null)
                {
                    m_PreviewObjects[obj].transform.position = obj.m_Translation;
                }
            }
        }
        private void DrawRotationTool(MapDataEntity.Object obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.m_Rotation = Handles.RotationHandle(obj.m_Rotation, obj.m_Translation);
            if (EditorGUI.EndChangeCheck())
            {
                //TreeObjectElement element = (TreeObjectElement)m_MapDataTreeView.I_Elements.FindFor((other) => other.TargetObject.Equals(obj));
                if (m_PreviewObjects[obj] != null)
                {
                    m_PreviewObjects[obj].transform.rotation = obj.m_Rotation;
                }
            }
        }
        #endregion

        #endregion
    }
}
