﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public class VerticalTreeView : VerticalTreeViewEntity
    {
        protected IList m_Data;
        protected Func<object, VerticalTreeElement> m_DataSetup;
        protected int m_CurrentDrawChilds = 0;

        public event Func<IList> OnAddButton;
        public event Func<VerticalTreeElement, IList> OnRemoveButton;

        public IList Data => m_Data;
        public int CurrentDrawChilds => m_CurrentDrawChilds;

        public VerticalTreeView(UnityEngine.Object asset, SerializedObject serializedObject) : base(asset, serializedObject) { }

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
        public VerticalTreeView Refresh(IList list)
        {
            m_Data = list;
            m_Elements.Clear();

            for (int i = 0; i < list.Count; i++)
            {
                m_Elements.Add(m_DataSetup.Invoke(list[i]));
            }
            SearchFieldChanged(null);
            return this;
        }
        public VerticalTreeView Refresh() => Refresh(Data);
        public VerticalTreeView MakeAddButton(Func<IList> onAdd)
        {
            m_DrawAddButton = true;
            OnAddButton += onAdd;
            return this;
        }
        public VerticalTreeView MakeRemoveButton(Func<VerticalTreeElement, IList> onRemove)
        {
            m_DrawRemoveButton = true;
            OnRemoveButton += onRemove;
            return this;
        }

        public virtual void OnGUI()
        {
            const string miniBtt = "miniButton";
            const string box = "Box";
            const string notFound = "Not Found";

            EditorGUILayout.BeginVertical(box);
            EditorUtils.Line();
            BeforeDraw();
            DrawToolbar();
            DrawSearchField();

            EditorGUILayout.BeginHorizontal();
            if (m_DrawAddButton && GUILayout.Button("+", miniBtt))
            {
                m_Data = OnAddButton?.Invoke();
                SetupElements(m_Data, m_DataSetup);
                EditorUtility.SetDirty(Asset);
            }
            if (m_DrawRemoveButton && GUILayout.Button("-", miniBtt))
            {
                m_Data = OnRemoveButton?.Invoke(m_Elements[m_Data.Count - 1]);
                SetupElements(m_Data, m_DataSetup);
                EditorUtility.SetDirty(Asset);
            }
            EditorGUILayout.EndHorizontal();
            EditorUtils.Line();

            Color originColor = GUI.backgroundColor;
            Color color1 = Color.black, color2 = Color.gray;
            color1.a = .5f; color2.a = .3f;

            m_CurrentDrawChilds = 0;
            BeforeDrawChilds();

            Color beforeColor;
            GUI.backgroundColor = color1;
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (m_Elements[i].HideElementInTree) continue;

                EditorGUILayout.BeginVertical(box);

                beforeColor = GUI.backgroundColor;
                GUI.backgroundColor = originColor;
                DrawChild(m_Elements[i]);
                GUI.backgroundColor = beforeColor;

                EditorGUILayout.EndVertical();
                if (m_Elements.Count <= i) continue;

                m_CurrentDrawChilds += 1;
                if (beforeColor == color1) GUI.backgroundColor = color2;
                else GUI.backgroundColor = color1;

                if (m_Elements[i].m_Opened && i + 1 < m_Elements.Count) EditorUtils.Line();
            }
            GUI.backgroundColor = originColor;
            if (m_CurrentDrawChilds == 0) EditorUtils.StringRich(notFound, true);
            AfterDraw();
            EditorUtils.Line();
            EditorGUILayout.EndVertical();
        }

        internal override void RemoveButtonClicked(VerticalTreeElement e)
        {
            int idx = -1;
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (m_Elements[i].Equals(e))
                {
                    idx = i;
                    break;
                }
            }
            if (idx < 0) throw new Exception($"{idx}");

            m_Data = OnRemoveButton?.Invoke(e);
            if (e.Parent != null) e.RemoveParent();

            SetupElements(m_Data, m_DataSetup);
            EditorUtility.SetDirty(Asset);
        }
    }
    public class VerticalTreeView<T> : VerticalTreeViewEntity where T : class
    {
        protected IList<T> m_Data;
        protected Func<T, VerticalTreeElement> m_DataSetup;

        public int m_CurrentDrawChilds = 0;
        public event Func<IList<T>> OnAddButton;
        public event Func<int, IList<T>> OnRemoveButton;

        public VerticalTreeView(UnityEngine.Object asset, SerializedObject serializedObject) : base(asset, serializedObject) { }

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
                if (m_Elements[i].HideElementInTree) continue;

                DrawChild(m_Elements[i]);
                if (m_Elements.Count <= i) continue;

                m_CurrentDrawChilds += 1;

                if (m_Elements[i].m_Opened && i + 1 < m_Elements.Count) EditorUtils.SectorLine();
            }
            if (m_CurrentDrawChilds == 0) EditorUtils.StringRich(notFound, true);
            AfterDraw();

            EditorGUILayout.EndVertical();
        }

        internal override void RemoveButtonClicked(VerticalTreeElement e)
        {
            int idx = -1;
            for (int i = 0; i < m_Elements.Count; i++)
            {
                if (m_Elements[i].Equals(e))
                {
                    idx = i;
                }
            }
            if (idx < 0) throw new Exception();

            m_Data = OnRemoveButton?.Invoke(idx);
            SetupElements(m_Data, m_DataSetup);
            EditorUtility.SetDirty(Asset);
        }
    }
}