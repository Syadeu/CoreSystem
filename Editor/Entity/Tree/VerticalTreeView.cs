using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public class VerticalTreeView
    {
        private UnityEngine.Object m_Asset;

        private SearchField m_SearchField;
        private string m_SearchString = null;

        private List<VerticalTreeElement> m_Elements;
        private Func<VerticalTreeElement, string, bool> m_CustomSearchFilter = null;

        private IList m_Data;

        #region Toolbar Values
        private bool m_EnableToolbar = false;
        private string[] m_ToolbarNames = null;
        private int m_SelectedToolbar = 0;
        #endregion

        public int m_CurrentDrawChilds = 0;

        public event Action<string> OnSearchFieldChanged;
        public event Action<int> OnToolbarChanged;
        public event Action<object> OnDeleteButton;

        public IReadOnlyList<VerticalTreeElement> Elements => m_Elements;

        public VerticalTreeView(UnityEngine.Object asset, IList data, params VerticalTreeElement[] elements)
        {
            m_Asset = asset;

            
            m_Data = data;

            m_SearchField = new SearchField();
            SetupElements(elements);

            OnInitialize();
        }
        public VerticalTreeView SetupElements(params VerticalTreeElement[] elements)
        {
            if (m_Elements == null) m_Elements = new List<VerticalTreeElement>();
            else m_Elements.Clear();

            m_Elements.AddRange(elements);

            SearchFieldChagned(in m_SearchString);
            return this;
        }
        public VerticalTreeView SetupElements<T>(IList<T> list, Func<T, VerticalTreeElement> func)
        {
            if (m_Elements == null) m_Elements = new List<VerticalTreeElement>();
            else m_Elements.Clear();

            for (int i = 0; i < list.Count; i++)
            {
                m_Elements.Add(func.Invoke(list[i]));
            }
            SearchFieldChagned(in m_SearchString);

            return this;
        }

        public void OnGUI()
        {
            const string box = "Box";
            const string notFound = "Not Found";

            EditorGUILayout.BeginVertical(box);

            BeforeDraw();
            DrawToolbar();
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
            if (m_CurrentDrawChilds == 0) EditorUtils.StringRich(notFound, true);
            AfterDraw();

            EditorGUILayout.EndVertical();
        }
        public void SetCustomSearchFilter(Func<VerticalTreeElement, string, bool> predicate)
        {
            m_CustomSearchFilter = predicate;
        }
        public VerticalTreeView MakeToolbar(params string[] toolbarNames)
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
        public VerticalTreeView MakeDeleteButton()
        {
            if (m_Data.IsFixedSize)
            {
                Debug.Log("Not supported list");
                return this;
            }

            OnDeleteButton += (data) =>
            {
                m_Data.Remove(data);
            };
            return this;
        }

        public void RemoveElements(params VerticalTreeElement[] elements)
        {
            m_Elements = m_Elements.Where((other) => !elements.Contains(other)).ToList();
        }

        protected virtual void OnInitialize() { }
        protected virtual void BeforeDraw() { }
        protected virtual void BeforeDrawChilds() { }
        protected virtual void AfterDraw() { }
        protected virtual void ToolbarChanged(ref int idx) { }

        private void DrawToolbar()
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

                if (e.m_Opened)
                {
                    e.OnGUI();

                    for (int i = 0; i < e.Childs.Count; i++)
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
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        m_Elements.Remove(e);
                        OnDeleteButton?.Invoke(e.Data);
                    }
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
    }

    public class VerticalTreeView<T> : VerticalTreeView where T : VerticalTreeElement
    {
        public VerticalTreeView(UnityEngine.Object asset, IList data, params VerticalTreeElement[] elements) 
            : base(asset, data, elements)
        {
        }

        //protected override void BeforeDraw()
        //{
        //    GUILayout.Toolbar(0, new string[] { "123", "456" });
        //}
    }
}