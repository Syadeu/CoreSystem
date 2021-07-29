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
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                EntityDataList.Instance.SaveData();
            }
            if (GUILayout.Button("Close"))
            {
                ResetAll();

                SceneView.lastActiveSceneView.Repaint();
                EditorGUILayout.EndHorizontal();

                Tools.hidden = false;
                return;
            }
            EditorGUILayout.EndHorizontal();
        }
        private void ResetAll()
        {
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

                PrefabReference prefab = mapData.m_Objects[i].m_Object.GetObject().Prefab;
                if (prefab.IsValid())
                {
                    var temp = prefab.GetObjectSetting().m_RefPrefab.editorAsset;

                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(temp, m_PreviewFolder);
                    obj.tag = c_EditorOnly;
                    obj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                    Transform tr = obj.transform;
                    tr.position = mapData.m_Objects[i].m_Translation;
                    tr.rotation = mapData.m_Objects[i].m_Rotation;
                    tr.localScale = mapData.m_Objects[i].m_Scale;

                    m_PreviewObjects.Add(mapData.m_Objects[i], obj);
                }
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
                    m_SceneData = new Reference<SceneDataEntity>(hash);

                    if (m_SceneData.IsValid())
                    {
                        m_SceneDataTarget = m_SceneData.GetObject();

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

            if (m_GridMap != null)
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

            using (new EditorUtils.BoxBlock(Color.black))
            {
                SaveNCloseButton();

                int screenWidth = Screen.width;
                m_MapDataScroll = GUILayout.BeginScrollView(m_MapDataScroll, false, false);

                m_MapDataTreeView.OnGUI();

                GUILayout.EndScrollView();
            }
        }
        private void MapDataSceneGUI(SceneView obj)
        {
            m_GridMap?.OnSceneGUI(obj);

            if (m_MapDataTarget == null) return;

            Color origin = Handles.color;
            Handles.color = Color.black;

            for (int i = 0; i < m_MapDataTarget.m_Objects?.Length; i++)
            {
                var objData = m_MapDataTarget.m_Objects[i].m_Object.GetObject();
                Handles.color = Color.white;

                string name = $"[{i}] {(objData != null ? $"{objData.Name}" : "None")}";

                Vector2 pos = HandleUtility.WorldToGUIPoint(m_MapDataTarget.m_Objects[i].m_Translation);
                if (!EditorSceneUtils.IsDrawable(pos)) continue;

                pos.x += 20;
                Rect rect = new Rect(pos, new Vector2(100, 50));

                Handles.BeginGUI();
                GUI.BeginGroup(rect, name, EditorUtils.Box);

                GUI.EndGroup();
                Handles.EndGUI();

                EditorGUI.BeginChangeCheck();

                switch (Tools.current)
                {
                    case Tool.View:
                        break;
                    case Tool.Move:
                        DrawMoveTool(m_MapDataTarget.m_Objects[i]);
                        break;
                    case Tool.Rotate:
                        DrawRotationTool(m_MapDataTarget.m_Objects[i]);
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
            }

            Handles.color = origin;

            int mouseControlID = GUIUtility.GetControlID(FocusType.Passive);
            switch (Event.current.GetTypeForControl(mouseControlID))
            {
                case EventType.MouseDown:
                    

                    if (Event.current.button == 2)
                    {
                        GUIUtility.hotControl = mouseControlID;

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

                                GameObject temp = (GameObject)refobj.GetObject().Prefab.GetObjectSetting().m_RefPrefab.editorAsset;
                                GameObject gameObj = (GameObject)PrefabUtility.InstantiatePrefab(temp, m_PreviewFolder);
                                gameObj.tag = c_EditorOnly;
                                gameObj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                                objData.m_Rotation = temp.transform.rotation;
                                objData.m_Scale = temp.transform.localScale;

                                m_PreviewObjects.Add(objData, gameObj);

                                List<MapDataEntity.Object> tempList = m_MapDataTarget.m_Objects.ToList();
                                tempList.Add(objData);
                                m_MapDataTarget.m_Objects = tempList.ToArray();

                                m_MapDataTreeView.Refresh(m_MapDataTarget.m_Objects);
                            },
                            (other) => other.Hash));

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
                    int idx = temp.IndexOf(target);

                    temp.RemoveAt(idx);
                    if (m_PreviewObjects[target] != null)
                    {
                        DestroyImmediate(m_PreviewObjects[target]);
                        m_PreviewObjects[target] = null;
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
                //TreeObjectElement element = (TreeObjectElement)m_MapDataTreeView.I_Elements.FindFor((other) => other.TargetObject.Equals(obj));
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
