using Syadeu;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Map;
using SyadeuEditor.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation.Map
{
    public sealed class MapDataWindow : EditorWindowEntity<MapDataWindow>
    {
        const string c_EditorOnly = "EditorOnly";

        protected override string DisplayName => "Map Data Window";

        private Reference<MapDataEntity> m_MapData;
        private MapDataEntity m_Target;
        private Transform m_PreviewFolder;

        private VerticalTreeView m_TreeView;

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

            base.OnDisable();
        }

        #region TreeView
        private void SetupTreeView(MapDataEntity data)
        {
            if (m_TreeView == null) m_TreeView = new VerticalTreeView(null, null);

            m_TreeView
                .SetupElements(data.m_Objects, (other) =>
                {
                    MapDataEntity.Object objData = (MapDataEntity.Object)other;

                    return new TreeObjectElement(m_TreeView, objData, m_PreviewFolder);
                })
                .MakeAddButton(() =>
                {
                    List<MapDataEntity.Object> temp = data.m_Objects.ToList();
                    
                    temp.Add(new MapDataEntity.Object());
                    
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
                    if (element.m_PreviewObject != null)
                    {
                        DestroyImmediate(element.m_PreviewObject);
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
                    else return temp.Name;
                }
            }
            private Transform m_Folder;
            public GameObject m_PreviewObject = null;

            public TreeObjectElement(VerticalTreeView treeView, MapDataEntity.Object target, Transform previewTr) : base(treeView, target)
            {
                m_Folder = previewTr;

                if (Target.m_Object.IsValid())
                {
                    PrefabReference prefab = Target.m_Object.GetObject().Prefab;
                    if (prefab.IsValid())
                    {
                        var temp = prefab.GetObjectSetting().m_RefPrefab.editorAsset;
                        
                        m_PreviewObject = (GameObject)PrefabUtility.InstantiatePrefab(temp, m_Folder);
                        m_PreviewObject.tag = c_EditorOnly;
                        m_PreviewObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                        m_PreviewObject.transform.position = Target.m_Translation;
                        m_PreviewObject.transform.rotation = Target.m_Rotation;

                        UpdatePreviewObject();
                    }
                }
            }
            public override void OnGUI()
            {
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    ReflectionHelperEditor.DrawReferenceSelector("Object",
                        (hash) =>
                        {
                            var target = new Reference<EntityBase>(hash);
                            if (!Target.m_Object.Equals(hash))
                            {
                                if (m_PreviewObject != null) DestroyImmediate(m_PreviewObject);

                                if (!hash.Equals(Hash.Empty))
                                {
                                    GameObject temp = target.GetObject().Prefab.GetObjectSetting().m_RefPrefab.editorAsset;

                                    m_PreviewObject = (GameObject)PrefabUtility.InstantiatePrefab(temp, m_Folder);
                                    m_PreviewObject.tag = c_EditorOnly;
                                    m_PreviewObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                                    Target.m_Rotation = temp.transform.rotation;
                                    Target.m_Scale = temp.transform.localScale;
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
                if (m_PreviewObject == null) return;

                Transform tr = m_PreviewObject.transform;
                tr.position = Target.m_Translation;
                tr.rotation = Target.m_Rotation;
                tr.localScale = Target.m_Scale;
            }
        }
        #endregion

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorUtils.StringHeader("Map Data Window", 20, true);
            GUILayout.Space(5);
            EditorUtils.Line();

            #region Map data selector
            using (new EditorUtils.BoxBlock(Color.gray))
            {
                ReflectionHelperEditor.DrawReferenceSelector("Map data: ", (hash) =>
                {
                    m_MapData = new Reference<MapDataEntity>(hash);
                }, m_MapData, TypeHelper.TypeOf<MapDataEntity>.Type);
            }
            #endregion

            EditorUtils.Line();

            if (Application.isPlaying)
            {
                EditorUtils.StringRich("Cannot edit data while playing", 13, true);
                return;
            }

            if (!m_MapData.IsValid())
            {
                EditorGUILayout.Space();
                EditorUtils.StringRich("Select map data", 13, true);
                return;
            }
            else if (m_Target == null)
            {
                m_Target = m_MapData.GetObject();
                SetupTreeView(m_Target);

                SceneView.lastActiveSceneView.Repaint();
            }

            if (GUILayout.Button("Save"))
            {
                EntityDataList.Instance.SaveData();
            }
            m_TreeView.OnGUI(); 
        }
        protected override void OnSceneGUI(SceneView obj)
        {
            if (m_Target == null) return;

            Color origin = Handles.color;
            Handles.color = Color.black;

            for (int i = 0; i < m_Target.m_Objects.Length; i++)
            {
                var objData = m_Target.m_Objects[i].m_Object.GetObject();
                Handles.color = Color.white;

                string name = $"[{i}] {(objData != null ? $"{objData.Name}" : "None")}";

                Vector2 pos = HandleUtility.WorldToGUIPoint(m_Target.m_Objects[i].m_Translation);
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
                        DrawMoveTool(m_Target.m_Objects[i]);
                        break;
                    case Tool.Rotate:
                        DrawRotationTool(m_Target.m_Objects[i]);
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
        private void DrawMoveTool(MapDataEntity.Object obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.m_Translation = Handles.PositionHandle(obj.m_Translation, obj.m_Rotation);
            if (EditorGUI.EndChangeCheck())
            {
                TreeObjectElement element = (TreeObjectElement)m_TreeView.I_Elements.FindFor((other) => other.TargetObject.Equals(obj));
                if (element.m_PreviewObject != null)
                {
                    element.m_PreviewObject.transform.position = obj.m_Translation;
                }
            }
        }
        private void DrawRotationTool(MapDataEntity.Object obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.m_Rotation = Handles.RotationHandle(obj.m_Rotation, obj.m_Translation);
            if (EditorGUI.EndChangeCheck())
            {
                TreeObjectElement element = (TreeObjectElement)m_TreeView.I_Elements.FindFor((other) => other.TargetObject.Equals(obj));
                if (element.m_PreviewObject != null)
                {
                    element.m_PreviewObject.transform.rotation = obj.m_Rotation;
                }
            }
        }
    }
}
