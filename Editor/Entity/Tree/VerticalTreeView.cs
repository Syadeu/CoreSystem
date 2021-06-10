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

        public VerticalTreeView(params VerticalTreeElement[] elements)
        {
            m_SearchField = new SearchField();
            SetupElements(elements);
        }
        private void SetupElements(params VerticalTreeElement[] elements)
        {
            List<VerticalTreeElement> temp = new List<VerticalTreeElement>(elements);
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                if (temp[i].Parent != null)
                {
                    if (!temp[i].m_Childs.Contains(temp[i]))
                    {
                        temp[i].m_Childs.Add(temp[i]);
                    }
                    temp.RemoveAt(i);
                }
            }

            if (m_Elements == null) m_Elements = new List<VerticalTreeElement>();
            else m_Elements.Clear();

            m_Elements.AddRange(temp);
        }

        public void OnGUI()
        {
            BeforeDraw();
            m_SearchString = m_SearchField.OnGUI(GUILayoutUtility.GetRect(Screen.width, 20), m_SearchString);
            BeforeDrawChilds();
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (!string.IsNullOrEmpty(m_SearchString))
                {
                    if (m_CustomSearchFilter != null)
                    {
                        if (!m_CustomSearchFilter.Invoke(m_Elements[i], m_SearchString)) continue;
                    }
                    else
                    {
                        if (!m_Elements[i].Name.Contains(m_SearchString)) continue;
                    }
                }
                DrawChild(m_Elements[i]);

                if (i + 1 < m_Elements.Count) EditorUtils.SectorLine();
            }
            AfterDraw();
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
            if (e.HasChilds)
            {
                e.m_Opened = EditorUtils.Foldout(e.m_Opened, e.Name);
                if (e.m_Opened)
                {
                    e.OnGUI();

                    EditorGUILayout.Space();

                    EditorGUI.indentLevel += 1;
                    for (int i = 0; i < e.Childs.Count; i++)
                    {
                        DrawChild(e.Childs[i]);
                    }
                    EditorGUI.indentLevel -= 1;
                }
            }
            else
            {
                e.OnGUI();
            }
        }

        public VerticalLabelTreeElement Label(string label) => new VerticalLabelTreeElement(this, label);
        public VerticalLabelTreeElement Label(string label1, string label2) => new VerticalLabelTreeElement(this, label1, label2);
    }
}