using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    /// <summary>
    /// Internal class for construct <see cref="VerticalTreeView"/>
    /// </summary>
    public abstract class VerticalTreeViewEntity
    {
        public bool m_DrawAddButton = false;
        public bool m_DrawRemoveButton = false;

        private UnityEngine.Object m_Asset;
        private SerializedObject m_SerializedObject;

        public event Action OnDirty;

        public UnityEngine.Object Asset => m_Asset;

        protected List<VerticalTreeElement> m_Elements = new List<VerticalTreeElement>();
        public IReadOnlyList<VerticalTreeElement> Elements => m_Elements;
        internal List<VerticalTreeElement> I_Elements => m_Elements;

        public int SelectedToolbar
        {
            get
            {
                //if (!m_EnableToolbar) throw new Exception();
                return m_SelectedToolbar;
            }
        }

        #region Search Values
        private readonly SearchField m_SearchField = new SearchField();
        private string m_SearchString = null;
        private Func<VerticalTreeElement, string, bool> m_CustomSearchFilter = null;

        public event Action<string> OnSearchFieldChanged;
        #endregion

        #region Toolbar Values
        public event Action<int> OnToolbarChanged;

        private bool m_EnableToolbar = false;
        private string[] m_ToolbarNames = null;
        private int m_SelectedToolbar = 0;
        #endregion

        public VerticalTreeViewEntity(UnityEngine.Object asset, SerializedObject serializedObject)
        {
            m_Asset = asset;
            m_SerializedObject = serializedObject;
        }

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
        public VerticalTreeViewEntity MakeToolbar(Action<int> onSelect, params string[] toolbarNames)
        {
            m_EnableToolbar = true;
            m_ToolbarNames = toolbarNames;

            OnToolbarChanged += onSelect;

            return this;
        }
        public VerticalFolderTreeElement GetOrCreateFolder(string name)
        {
            name = name.Trim();

            var temp = m_Elements.Where((other) => (other is VerticalFolderTreeElement) && other.Name.Equals(name));

            VerticalFolderTreeElement output;
            if (!temp.Any())
            {
                output = new VerticalFolderTreeElement(this, name);
                m_Elements.Add(output);
            }
            else output = temp.First() as VerticalFolderTreeElement;

            return output;
        }
        public VerticalFolderTreeElement GetOrCreateFolder<T>(string name) where T : VerticalFolderTreeElement, new()
        {
            name = name.Trim();

            var temp = m_Elements.Where((other) => (other is T) && other.Name.Equals(name));

            VerticalFolderTreeElement output;
            if (!temp.Any())
            {
                output = new T();
                output.m_Tree = this;
                output.m_Name = name;

                m_Elements.Add(output);
            }
            else output = temp.First() as VerticalFolderTreeElement;

            return output;
        }
        public void AddElement<T>(T element) where T : VerticalTreeElement
        {
            m_Elements.Add(element);
        }

        protected virtual void BeforeDraw() { }
        protected virtual void BeforeDrawChilds() { }
        protected virtual void AfterDraw() { }
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
            if (!string.IsNullOrEmpty(m_SearchString))
            {
                if (ValidateDrawParentChild(e))
                {
                    e.Expend();
                }
                else return;
            }

            if (e.HasChilds)
            {
                EditorGUI.indentLevel += 1;
                e.m_Opened = EditorUtilities.Foldout(e.m_Opened, e.Name);

                if (e.m_Opened)
                {
                    EditorGUI.BeginChangeCheck();
                    e.OnGUI();
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(m_Asset);
                        OnDirty?.Invoke();
                    }

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

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        e.m_Opened = EditorUtilities.Foldout(e.m_Opened, $"{e.Name}", 12);
                        if (m_DrawRemoveButton && DrawRemoveButton(e))
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.indentLevel -= 1;
                            return;
                        }
                    }

                    EditorGUI.indentLevel -= 1;
                    if (!e.m_Opened) return;
                }
                else
                {
                    EditorUtilities.StringRich($"{e.Name}", 12);
                }

                EditorGUI.indentLevel += 1;
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    e.OnGUI();
                    if (change.changed && m_Asset != null) EditorUtility.SetDirty(m_Asset);
                }
                
                EditorGUI.indentLevel -= 1;
            }
        }
        internal protected void DrawSearchField()
        {
            Rect rect = GUILayoutUtility.GetLastRect();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_SearchString = m_SearchField.OnGUI(GUILayoutUtility.GetRect(rect.width, 20), m_SearchString);
                if (change.changed)
                {
                    SearchFieldChanged(in m_SearchString);
                    OnSearchFieldChanged?.Invoke(m_SearchString);
                }
            }
        }
        internal protected bool DrawRemoveButton(VerticalTreeElement e)
        {
            const string miniBtt = "miniButton";

            if (GUILayout.Button("-", miniBtt, GUILayout.Width(20)))
            {
                RemoveButtonClicked(e);
                e.Remove();
                if (m_Asset != null) EditorUtility.SetDirty(m_Asset);

                return true;
            }
            return false;
        }
        internal abstract void RemoveButtonClicked(VerticalTreeElement e);

        #region Validate
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
                if (child.Name.ToLower().Contains(m_SearchString.ToLower())) return true;
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
        #endregion
    }
}