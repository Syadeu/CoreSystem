using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public class VerticalTreeView : VerticalTreeViewEntity
    {
        protected IList m_Data;
        protected Func<object, VerticalTreeElement> m_DataSetup;

        public int m_CurrentDrawChilds = 0;
        public event Func<IList> OnAddButton;

        public VerticalTreeView(UnityEngine.Object asset) : base(asset) { }

        public VerticalTreeView SetupElements(IList list, Func<object, VerticalTreeElement> func)
        {
            m_Data = list;
            m_DataSetup = func;

            m_Elements.Clear();

            for (int i = 0; i < list.Count; i++)
            {
                m_Elements.Add(func.Invoke(list[i]));
            }
            SearchFieldChanged(null);
            return this;
        }

        public virtual void OnGUI()
        {
            const string box = "Box";
            const string notFound = "Not Found";

            EditorGUILayout.BeginVertical(box);

            BeforeDraw();
            DrawToolbar();
            DrawSearchField();

            if (GUILayout.Button("+"))
            {
                m_Data = OnAddButton?.Invoke();
                SetupElements(m_Data, m_DataSetup);
                EditorUtility.SetDirty(Asset);
            }

            m_CurrentDrawChilds = 0;
            BeforeDrawChilds();
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (m_Elements[i].m_HideElementInTree) continue;

                DrawChild(m_Elements[i]);
                if (m_Elements[i].m_Disposed)
                {
                    m_Elements.RemoveAt(i);
                    i--;
                    continue;
                }

                m_CurrentDrawChilds += 1;

                if (m_Elements[i].m_Opened && i + 1 < m_Elements.Count) EditorUtils.SectorLine();
            }
            if (m_CurrentDrawChilds == 0) EditorUtils.StringRich(notFound, true);
            AfterDraw();

            EditorGUILayout.EndVertical();
        }
    }
    public class VerticalTreeView<T> : VerticalTreeViewEntity where T : class
    {
        protected IList<T> m_Data;
        protected Func<T, VerticalTreeElement> m_DataSetup;

        public int m_CurrentDrawChilds = 0;
        public event Func<IList<T>> OnAddButton;

        public VerticalTreeView(UnityEngine.Object asset) : base(asset) { }

        public VerticalTreeView<T> SetupElements(IList<T> list, Func<T, VerticalTreeElement> func)
        {
            m_Data = list;
            m_DataSetup = func;

            m_Elements.Clear();

            for (int i = 0; i < list.Count; i++)
            {
                m_Elements.Add(func.Invoke(list[i]));
            }
            SearchFieldChanged(null);
            return this;
        }

        public virtual void OnGUI()
        {
            const string box = "Box";
            const string notFound = "Not Found";

            EditorGUILayout.BeginVertical(box);

            BeforeDraw();
            DrawToolbar();
            DrawSearchField();

            if (GUILayout.Button("+"))
            {
                m_Data = OnAddButton?.Invoke();
                SetupElements(m_Data, m_DataSetup);
                EditorUtility.SetDirty(Asset);
            }

            m_CurrentDrawChilds = 0;
            BeforeDrawChilds();
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (m_Elements[i].m_HideElementInTree) continue;

                DrawChild(m_Elements[i]);
                if (m_Elements[i].m_Disposed)
                {
                    m_Elements.RemoveAt(i);
                    i--;
                    continue;
                }

                m_CurrentDrawChilds += 1;

                if (m_Elements[i].m_Opened && i + 1 < m_Elements.Count) EditorUtils.SectorLine();
            }
            if (m_CurrentDrawChilds == 0) EditorUtils.StringRich(notFound, true);
            AfterDraw();

            EditorGUILayout.EndVertical();
        }
    }
}