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
            EditorUtils.StringHeader("Map System", 20, true);
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
            // GridMapAttribute
            m_SceneDataGridAtt = null;
            if (m_SceneDataGrid != null) m_SceneDataGrid.Dispose();
            m_SceneDataGrid = null;
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

        // GridMapAttribute
        private GridMapAttribute m_SceneDataGridAtt;
        private ManagedGrid m_SceneDataGrid;

        private bool m_debug = false;

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
                        m_SceneDataGridAtt = m_SceneDataTarget.GetAttribute<GridMapAttribute>();

                        if (m_SceneDataGrid != null) m_SceneDataGrid.Dispose();
                        m_SceneDataGrid = new ManagedGrid(m_SceneDataGridAtt.m_Center, m_SceneDataGridAtt.m_Size, m_SceneDataGridAtt.m_CellSize);

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

            m_SceneDataScroll = GUILayout.BeginScrollView(m_SceneDataScroll, false, false);
            if (m_SceneDataTarget != null)
            {
                if (m_SceneDataGridAtt != null)
                {
                    using (new EditorUtils.BoxBlock(Color.gray))
                    {
                        EditorUtils.StringRich("GridMap", 13);
                        EditorGUI.BeginDisabledGroup(true);
                        ReflectionHelperEditor.DrawObject(m_SceneDataGridAtt);
                        EditorGUI.EndDisabledGroup();
                    }

                    EditorGUILayout.LabelField($"{m_SceneDataGrid.gridSize}");

                    m_debug = EditorGUILayout.Toggle(m_debug);
                }
                EditorUtils.Line();
            }
            GUILayout.EndScrollView();
        }
        private void SceneDataSceneGUI(SceneView obj)
        {
            if (m_SceneDataGrid == null) return;

            //Selection.activeObject = null;
            m_SceneDataGrid.DrawGL();
            Handles.DrawWireCube(m_SceneDataGrid.bounds.center, m_SceneDataGrid.size);

            if (!m_debug) return;

            int mouseControlID = GUIUtility.GetControlID(FocusType.Passive);
            switch (Event.current.GetTypeForControl(mouseControlID))
            {
                case EventType.MouseDown:
                    GUIUtility.hotControl = mouseControlID;

                    Ray ray = EditorSceneUtils.GetMouseScreenRay();
                    if (m_SceneDataGrid.bounds.Intersect(ray, out float dis, out var point))
                    {
                        $"{dis} :: {point}".ToLog();

                        var pos = m_SceneDataGrid.GetCellPosition(point);

                        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        temp.transform.SetParent(m_PreviewFolder);
                        temp.transform.position = pos;
                    }

                    //if (Event.current.button == 0)
                    //{
                        
                    //}
                    //else if (Event.current.button == 1)
                    //{
                        
                    //}

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

        #endregion

        #region Map Data

        private Reference<MapDataEntity> m_MapData;
        private MapDataEntity m_MapDataTarget;
        private Vector2 m_MapDataScroll;
        private VerticalTreeView m_MapDataTreeView;
        private void MapDataGUI()
        {
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

            if (!m_MapData.IsValid())
            {
                EditorGUILayout.Space();
                EditorUtils.StringRich("Select map data", 13, true);
                return;
            }

            SaveNCloseButton();

            int screenWidth = Screen.width;
            m_MapDataScroll = GUILayout.BeginScrollView(m_MapDataScroll, false, false);

            m_MapDataTreeView.OnGUI();

            GUILayout.EndScrollView();
        }
        private void MapDataSceneGUI(SceneView obj)
        {
            if (m_MapDataTarget == null) return;

            Color origin = Handles.color;
            Handles.color = Color.black;

            for (int i = 0; i < m_MapDataTarget.m_Objects?.Length; i++)
            {
                var objData = m_MapDataTarget.m_Objects[i].m_Object.GetObject();
                Handles.color = Color.white;

                string name = $"[{i}] {(objData != null ? $"{objData.Name}" : "None")}";

                Vector2 pos = HandleUtility.WorldToGUIPoint(m_MapDataTarget.m_Objects[i].m_Translation);
                pos.x += 20;
                Rect rect = new Rect(pos, new Vector2(100, 50));

                Handles.BeginGUI();
                GUI.BeginGroup(rect, name, EditorUtils.Box);

                //GUI.Label(rect, "tesasdasdasdasdt");
                //GUILayout.Label("gfhghfhfggfh");
                //EditorGUILayout.LabelField("dasiduiouxoi");

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
            //private Transform m_Folder;
            //public GameObject m_PreviewObject = null;
            //private int m_Idx;

            readonly Transform m_Folder;
            readonly Dictionary<MapDataEntity.Object, GameObject> m_List;

            public TreeObjectElement(VerticalTreeView treeView, MapDataEntity.Object target, Transform folder, Dictionary<MapDataEntity.Object, GameObject> list) : base(treeView, target)
            {
                m_Folder = folder;
                m_List = list;
                //m_Idx = treeView.Data.IndexOf(target);

                //if (Target.m_Object.IsValid())
                //{
                //    PrefabReference prefab = Target.m_Object.GetObject().Prefab;
                //    if (prefab.IsValid())
                //    {
                //        var temp = prefab.GetObjectSetting().m_RefPrefab.editorAsset;

                //        m_PreviewObject = (GameObject)PrefabUtility.InstantiatePrefab(temp, m_Folder);
                //        m_PreviewObject.tag = c_EditorOnly;
                //        m_PreviewObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                //        m_PreviewObject.transform.position = Target.m_Translation;
                //        m_PreviewObject.transform.rotation = Target.m_Rotation;

                //        UpdatePreviewObject();
                //    }
                //}
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
                                    GameObject temp = target.GetObject().Prefab.GetObjectSetting().m_RefPrefab.editorAsset;

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
