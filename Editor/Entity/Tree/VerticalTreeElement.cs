using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public abstract class VerticalTreeElement
    {
        public bool m_EnableFoldout = true;

        [SerializeField] protected string m_Name;

        [NonSerialized] private VerticalTreeView m_Tree;
        [NonSerialized] internal VerticalTreeElement m_Parent;
        [NonSerialized] internal List<VerticalTreeElement> m_Childs;
        [NonSerialized] internal bool m_HideElementInTree = false;

        internal bool m_Opened = false;

        public virtual string Name => m_Name;
        public abstract object Data { get; }

        public bool HasChilds => Childs != null && Childs.Count > 0;

        public VerticalTreeElement Parent => m_Parent;
        public IReadOnlyList<VerticalTreeElement> Childs => m_Childs;

        public VerticalTreeElement(VerticalTreeView tree)
        {
            m_Tree = tree;
        }

        public abstract void OnGUI();
        public void SetParent(VerticalTreeElement parent)
        {
            if (m_Parent != null)
            {
                RemoveParent();
            }
            else m_Tree.RemoveElements(this);

            if (parent.m_Childs == null) parent.m_Childs = new List<VerticalTreeElement>();
            parent.m_Childs.Add(this);
            m_Parent = parent;
        }
        public void RemoveParent()
        {
            if (m_Parent == null) return;

            if (m_Parent.Childs.Count == 1)
            {
                m_Parent.m_Childs = null;
            }
            else m_Parent.m_Childs.Remove(this);
            m_Parent = null;
        }
        public void Expend()
        {
            m_Opened = true;
            if (Parent != null) Parent.Expend();
        }

        protected Rect GetRect(float width, float height) => GUILayoutUtility.GetRect(width, height);
    }
}