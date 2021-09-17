using Syadeu.Presentation;
using Syadeu.Presentation.Map;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class MapDataEntityDrawer : EntityDrawer
    {
        private bool m_OpenInvalidList = false;
        private readonly List<int> m_InvalidIndices = new List<int>();
        private readonly List<bool> m_OpenInvalidIndex = new List<bool>();

        public override string Name
        {
            get
            {
                if (m_InvalidIndices.Count > 0)
                {
                    return base.Name + " [Contains Invalid Objects]";
                }
                return base.Name;
            }
        }
        MapDataEntityBase Entity => (MapDataEntityBase)Target;
        ArrayDrawer ArrayDrawer;

        public MapDataEntityDrawer(ObjectBase objectBase) : base(objectBase)
        {
            FindInvalidObjectIndices();

            //ArrayDrawer = (ArrayDrawer)Drawers.Where((other) => other.Name.Equals("Objects")).First();
            ArrayDrawer = GetDrawer<ArrayDrawer>("Objects");
        }

        private static bool IsInvalidObject(MapDataEntityBase.Object obj)
        {
            if (!obj.m_Object.IsValid() || obj.m_Object.IsEmpty())
            {
                return true;
            }
            return false;
        }
        private void FindInvalidObjectIndices()
        {
            m_InvalidIndices.Clear();
            m_OpenInvalidIndex.Clear();

            for (int i = 0; i < Entity.m_Objects.Length; i++)
            {
                MapDataEntityBase.Object obj = Entity.m_Objects[i];
                if (IsInvalidObject(obj))
                {
                    obj.m_Object = Reference<Syadeu.Presentation.Entities.EntityBase>.Empty;
                    GUI.changed = true;

                    m_InvalidIndices.Add(i);
                    m_OpenInvalidIndex.Add(false);
                }
            }

            m_OpenInvalidList = m_InvalidIndices.Count > 0;
        }
        private void DrawInvalids()
        {
            using (new EditorUtils.BoxBlock(ColorPalettes.TriadicColor.Three))
            {
                m_OpenInvalidList = EditorUtils.Foldout(m_OpenInvalidList, "Invalid Objects Founded", 13);
                if (GUILayout.Button("Remove All"))
                {
                    List<MapDataEntityBase.Object> temp = Entity.m_Objects.ToList();
                    temp.RemoveAll(IsInvalidObject);
                    Entity.m_Objects = temp.ToArray();

                    List<ObjectDrawerBase> removeList = new List<ObjectDrawerBase>();
                    for (int i = 0; i < m_InvalidIndices.Count; i++)
                    {
                        removeList.Add(ArrayDrawer.m_ElementDrawers[m_InvalidIndices[i]]);
                    }
                    for (int i = 0; i < removeList.Count; i++)
                    {
                        ArrayDrawer.m_ElementDrawers.Remove(removeList[i]);
                    }

                    m_InvalidIndices.Clear();
                    m_OpenInvalidIndex.Clear();
                    GUI.changed = true;
                }

                if (!m_OpenInvalidList) return;

                EditorGUILayout.HelpBox(
                    $"Number({m_InvalidIndices.Count}) of invalid objects in this data were found.\n" +
                    $"Fix it manually."
                    , MessageType.Error);

                EditorGUI.indentLevel++;
                for (int i = 0; i < m_InvalidIndices.Count; i++)
                {
                    int idx = m_InvalidIndices[i];

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        m_OpenInvalidIndex[i]
                        = EditorUtils.Foldout(m_OpenInvalidIndex[i], $"Element At {idx}");
                        if (GUILayout.Button("Remove", GUILayout.Width(100)))
                        {
                            var temp = Entity.m_Objects.ToList();
                            temp.RemoveAt(idx);
                            Entity.m_Objects = temp.ToArray();

                            ArrayDrawer.m_ElementDrawers.RemoveAt(idx);

                            m_InvalidIndices.RemoveAt(i);
                            m_OpenInvalidIndex.RemoveAt(i);
                            i--;

                            GUI.changed = true;
                            continue;
                        }
                    }

                    if (m_OpenInvalidIndex[i])
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        ArrayDrawer.m_ElementDrawers[idx].OnGUI();
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (!IsInvalidObject(Entity.m_Objects[idx]))
                            {
                                m_InvalidIndices.RemoveAt(i);
                                m_OpenInvalidIndex.RemoveAt(i);
                                i--;
                            }
                        }
                        EditorGUI.indentLevel--;
                    }

                    if (i + 1 < m_InvalidIndices.Count) EditorUtils.Line();
                }
                EditorGUI.indentLevel--;
            }
        }
        protected override void DrawGUI()
        {
            DrawHeader();
            EditorUtils.Line();

            if (m_InvalidIndices.Count > 0)
            {
                DrawInvalids();
                EditorUtils.Line();
            }

            for (int i = 0; i < Drawers.Length; i++)
            {
                if (!IsDrawable(Drawers[i]))
                {
                    continue;
                }

                DrawField(Drawers[i]);
            }
        }
    }
    //[EditorTool("TestTool", typeof(EntityWindow))]
    //public sealed class TestTool : EditorTool
    //{
    //    public override void OnToolGUI(EditorWindow window)
    //    {
    //        EditorGUILayout.LabelField("test");
    //        base.OnToolGUI(window);
    //    }
    //}
}
