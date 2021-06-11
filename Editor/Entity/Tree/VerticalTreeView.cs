using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public class VerticalTreeView : VerticalTreeViewEntity
    {
        protected UnityEngine.Object m_Asset;
        protected IList m_Data;
        protected Func<object, VerticalTreeElement> m_DataSetup;

        public int m_CurrentDrawChilds = 0;

        public event Func<IList> OnAddButton;

        
        
        public VerticalTreeView(UnityEngine.Object asset)
        {
            m_Asset = asset;
            OnInitialize();
        }
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

        public void OnGUI()
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