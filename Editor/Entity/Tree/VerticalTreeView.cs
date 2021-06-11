using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public class VerticalTreeView
    {
        private SearchField m_SearchField;
        private string m_SearchString = null;

        private List<VerticalTreeElement> m_Elements;
        private Func<VerticalTreeElement, string, bool> m_CustomSearchFilter = null;

        public int m_CurrentDrawChilds = 0;
        public event Action<string> OnSearchFieldChanged;

        public IReadOnlyList<VerticalTreeElement> Elements => m_Elements;

        public VerticalTreeView(params VerticalTreeElement[] elements)
        {
            m_SearchField = new SearchField();
            SetupElements(elements);
        }
        public void SetupElements(params VerticalTreeElement[] elements)
        {
            if (m_Elements == null) m_Elements = new List<VerticalTreeElement>();
            else m_Elements.Clear();

            m_Elements.AddRange(elements);

            SearchFieldChagned(in m_SearchString);
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");

            BeforeDraw();
            DrawSearchField();

            m_CurrentDrawChilds = 0;
            BeforeDrawChilds();
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (m_Elements[i].m_HideElementInTree) continue;

                DrawChild(m_Elements[i]);
                m_CurrentDrawChilds += 1;

                if (m_Elements[i].m_Opened && i + 1 < m_Elements.Count) EditorUtils.SectorLine();
            }
            if (m_CurrentDrawChilds == 0) EditorUtils.StringRich("Not Found", true);
            AfterDraw();

            EditorGUILayout.EndVertical();
        }
        public void SetCustomSearchFilter(Func<VerticalTreeElement, string, bool> predicate)
        {
            m_CustomSearchFilter = predicate;
        }
        public void RemoveElements(params VerticalTreeElement[] elements)
        {
            m_Elements = m_Elements.Where((other) => !elements.Contains(other)).ToList();
        }

        protected virtual void BeforeDraw() { }
        protected virtual void BeforeDrawChilds() { }
        protected virtual void AfterDraw() { }

        private void DrawChild(VerticalTreeElement e)
        {
            if (ValidateDrawParentChild(e))
            {
                if (!string.IsNullOrEmpty(m_SearchString))
                {
                    e.Expend();
                }
            }

            if (e.HasChilds)
            {
                EditorGUI.indentLevel += 1;
                e.m_Opened = EditorUtils.Foldout(e.m_Opened, e.Name);
                EditorGUI.indentLevel -= 1;
                if (e.m_Opened)
                {
                    EditorGUI.indentLevel += 1;
                    e.OnGUI();

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Childs");

                    for (int i = 0; i < e.Childs.Count; i++)
                    {
                        DrawChild(e.Childs[i]);
                    }
                    EditorGUI.indentLevel -= 1;
                }
            }
            else
            {
                if (e.m_EnableFoldout)
                {
                    EditorGUI.indentLevel += 1;
                    e.m_Opened = EditorUtils.Foldout(e.m_Opened, $"{e.Name}", 12);
                    EditorGUI.indentLevel -= 1;
                    if (!e.m_Opened) return;
                }
                else
                {
                    EditorUtils.StringRich($"{e.Name}", 12);
                }

                EditorGUI.indentLevel += 1;
                e.OnGUI();
                EditorGUI.indentLevel -= 1;
            }
        }
        private void DrawSearchField()
        {
            EditorGUI.BeginChangeCheck();
            m_SearchString = m_SearchField.OnGUI(GUILayoutUtility.GetRect(Screen.width, 20), m_SearchString);
            if (EditorGUI.EndChangeCheck())
            {
                SearchFieldChagned(in m_SearchString);
                OnSearchFieldChanged?.Invoke(m_SearchString);
            }
        }
        private bool ValidateDrawParent(VerticalTreeElement e)
        {
            if (e.Parent != null) return false;
            
            return ValidateDrawParentChild(e);
        }
        private bool ValidateDrawParentChild(VerticalTreeElement child)
        {
            if (string.IsNullOrEmpty(m_SearchString)) return true;
            if (m_CustomSearchFilter != null)
            {
                if (m_CustomSearchFilter.Invoke(child, m_SearchString)) return true;
            }
            else
            {
                if (child.Name.Contains(m_SearchString)) return true;
            }

            if (child.HasChilds)
            {
                for (int i = 0; i < child.Childs.Count; i++)
                {
                    if (ValidateDrawParentChild(child.Childs[i])) return true;
                }
            }
            return false;
        }

        protected virtual void SearchFieldChagned(in string field)
        {
            for (int i = 0; i < m_Elements.Count; i++)
            {
                m_Elements[i].m_HideElementInTree = !ValidateDrawParent(m_Elements[i]);
            }
        }

        public VerticalLabelTreeElement Label(string label) => new VerticalLabelTreeElement(this, label);
        public VerticalLabelTreeElement Label(string label1, string label2) => new VerticalLabelTreeElement(this, label1, label2);
    }

    public class VerticalTreeView<T> : VerticalTreeView where T : VerticalTreeElement
    {

    }
}