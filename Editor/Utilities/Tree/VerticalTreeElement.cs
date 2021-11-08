﻿using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEditor;

namespace SyadeuEditor.Tree
{
    public abstract class VerticalTreeElement : IDisposable
    {
        public bool m_EnableFoldout = true;

        public string m_Name;

        [NonSerialized] internal VerticalTreeViewEntity m_Tree;
        [NonSerialized] internal VerticalTreeElement m_Parent;
        [NonSerialized] internal List<VerticalTreeElement> m_Childs;
        [NonSerialized] internal bool m_HideElementInTree = false;

        [NonSerialized] internal bool m_Opened = false;
        [NonSerialized] internal bool m_Disposed = false;

        public VerticalTreeViewEntity Tree => m_Tree;
        public virtual string Name => m_Name;

        public virtual bool HideElementInTree => m_HideElementInTree;
        public bool HasChilds => Childs != null && Childs.Count > 0;

        public abstract object TargetObject { get; }
        public VerticalTreeElement Parent => m_Parent;
        public IReadOnlyList<VerticalTreeElement> Childs => m_Childs;

        public VerticalTreeElement() { }
        public VerticalTreeElement(VerticalTreeViewEntity tree)
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
            //else m_Tree.RemoveElements(this);

            if (parent.m_Childs == null) parent.m_Childs = new List<VerticalTreeElement>();
            parent.m_Childs.Add(this);
            m_Parent = parent;
        }
        internal void RemoveParent()
        {
            if (m_Parent == null) return;

            if (m_Parent.Childs.Count == 1)
            {
                m_Parent.m_Childs = null;
            }
            else m_Parent.m_Childs.Remove(this);
            m_Parent = null;
        }
        internal void Remove()
        {
            //OnRemove();

            RemoveParent();
            Dispose();
        }
        public void Expend()
        {
            m_Opened = true;
            if (Parent != null) Parent.Expend();
        }

        //protected abstract void OnRemove();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            m_Disposed = true;
        }

        protected static Rect GetRect(float width, float height) => GUILayoutUtility.GetRect(width, height);
    }
    public abstract class VerticalTreeElement<T> : VerticalTreeElement where T : class
    {
        private T m_Target;
        //internal SerializedProperty m_Property;

        public override object TargetObject => m_Target;
        public T Target => m_Target;

        public VerticalTreeElement(VerticalTreeView tree, T target) : base(tree)
        {
            m_Target = target;
        }
    }
}