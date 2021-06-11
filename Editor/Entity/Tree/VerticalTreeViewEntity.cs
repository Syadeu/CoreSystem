﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public abstract class VerticalTreeViewEntity
    {
        protected List<VerticalTreeElement> m_Elements = new List<VerticalTreeElement>();
        public IReadOnlyList<VerticalTreeElement> Elements => m_Elements;
        internal List<VerticalTreeElement> I_Elements => m_Elements;

        #region Search Values
        private readonly SearchField m_SearchField = new SearchField();
        private string m_SearchString = null;
        private Func<VerticalTreeElement, string, bool> m_CustomSearchFilter = null;

        public event Action<string> OnSearchFieldChanged;
        public event Action<int> OnToolbarChanged;
        #endregion

        #region Toolbar Values
        private bool m_EnableToolbar = false;
        private string[] m_ToolbarNames = null;
        private int m_SelectedToolbar = 0;
        #endregion

        public void MakeCustomSearchFilter(Func<VerticalTreeElement, string, bool> predicate)
        {
            m_CustomSearchFilter = predicate;
        }
        public VerticalTreeViewEntity MakeToolbar(params string[] toolbarNames)
        {
            m_EnableToolbar = true;
            m_ToolbarNames = toolbarNames;

            return this;
        }
        public VerticalFolderTreeElement GetOrCreateFolder(string name)
        {
            var temp = m_Elements.Where((other) => (other is VerticalFolderTreeElement) && other.Name.Equals(name));

            VerticalFolderTreeElement output;
            if (temp.Count() == 0)
            {
                output = new VerticalFolderTreeElement(this, name);
                m_Elements.Add(output);
            }
            else output = temp.First() as VerticalFolderTreeElement;

            return output;
        }

        protected virtual void OnInitialize() { }
        protected virtual void BeforeDraw() { }
        protected virtual void BeforeDrawChilds() { }
        protected virtual void AfterDraw() { }
        protected virtual void OnDrawFoldout(VerticalTreeElement e) { }
        protected virtual void ToolbarChanged(ref int idx) { }
        protected virtual void SearchFieldChanged(in string field)
        {
            for (int i = 0; i < m_Elements.Count; i++)
            {
                m_Elements[i].m_HideElementInTree = !ValidateDrawParent(m_Elements[i]);
            }
        }

        internal void RemoveElements(params VerticalTreeElement[] elements)
        {
            m_Elements = m_Elements.Where((other) => !elements.Contains(other)).ToList();
        }
        internal protected void DrawToolbar()
        {
            if (!m_EnableToolbar) return;

            EditorGUI.BeginChangeCheck();
            m_SelectedToolbar = GUILayout.Toolbar(m_SelectedToolbar, m_ToolbarNames);
            if (EditorGUI.EndChangeCheck())
            {
                ToolbarChanged(ref m_SelectedToolbar);
                OnToolbarChanged?.Invoke(m_SelectedToolbar);
            }
        }
        internal protected void DrawChild(VerticalTreeElement e)
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

                if (e.m_Opened)
                {
                    e.OnGUI();

                    for (int i = 0; i < e.Childs?.Count; i++)
                    {
                        DrawChild(e.Childs[i]);
                    }
                }

                EditorGUI.indentLevel -= 1;
            }
            else
            {
                if (e.m_EnableFoldout)
                {
                    EditorGUI.indentLevel += 1;

                    EditorGUILayout.BeginHorizontal();
                    e.m_Opened = EditorUtils.Foldout(e.m_Opened, $"{e.Name}", 12);
                    if (DrawRemoveButton(e))
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel -= 1;
                        return;
                    }
                    OnDrawFoldout(e);
                    EditorGUILayout.EndHorizontal();

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
        internal protected void DrawSearchField()
        {
            EditorGUI.BeginChangeCheck();
            m_SearchString = m_SearchField.OnGUI(GUILayoutUtility.GetRect(Screen.width, 20), m_SearchString);
            if (EditorGUI.EndChangeCheck())
            {
                SearchFieldChanged(in m_SearchString);
                OnSearchFieldChanged?.Invoke(m_SearchString);
            }
        }
        internal protected bool DrawRemoveButton(VerticalTreeElement e)
        {
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                e.Remove();
                return true;
            }
            return false;
        }
        internal protected bool ValidateDrawParent(VerticalTreeElement e)
        {
            if (e.Parent != null) return false;

            return ValidateDrawParentChild(e);
        }
        internal protected bool ValidateDrawParentChild(VerticalTreeElement child)
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
    }
}