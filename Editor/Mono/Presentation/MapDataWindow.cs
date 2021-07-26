using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using SyadeuEditor.Tree;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    public sealed class MapDataWindow : EditorWindowEntity<MapDataWindow>
    {
        protected override string DisplayName => "Map Data Window";

        private Reference<MapDataEntity> m_MapData;
        private MapDataEntity m_Target;
        private Transform m_PreviewFolder;

        private VerticalTreeView m_TreeView;

        protected override void OnEnable()
        {
            m_PreviewFolder = new GameObject("Preview").transform;
            m_PreviewFolder.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

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

                    return new TreeObjectElement(m_TreeView, objData);
                })
                .MakeAddButton(() =>
                {
                    var temp = data.m_Objects.ToList();
                    temp.Add(new MapDataEntity.Object());

                    data.m_Objects = temp.ToArray();
                    return data.m_Objects;
                })
                .MakeRemoveButton((other) =>
                {
                    var temp = data.m_Objects.ToList();
                    temp.Remove((MapDataEntity.Object)other.TargetObject);

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

            public TreeObjectElement(VerticalTreeView treeView, MapDataEntity.Object target) : base(treeView, target)
            {

            }
            public override void OnGUI()
            {
                using (new EditorUtils.BoxBlock(Color.black))
                {
                    ReflectionHelperEditor.DrawObject(Target);
                }
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

            //using (new EditorUtils.BoxBlock(Color.gray))
            //{


            //    for (int i = 0; i < m_Target.m_Objects.Length; i++)
            //    {
            //        m_Target.m_Objects[i].m_Translation =
            //            EditorGUILayout.Vector3Field("Position: ", m_Target.m_Objects[i].m_Translation);
            //    }
            //}
        }
        protected override void OnSceneGUI(SceneView obj)
        {
            if (m_Target == null) return;

            //Handles.BeginGUI();
            for (int i = 0; i < m_Target.m_Objects.Length; i++)
            {
                EditorGUI.BeginChangeCheck();

                m_Target.m_Objects[i].m_Translation =
                    Handles.PositionHandle(m_Target.m_Objects[i].m_Translation, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    Repaint();
                }
            }
            //Handles.EndGUI();
        }
    }
}
