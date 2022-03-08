using System;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public abstract class EntityWindowMenuItem : IDisposable, IComparable<EntityWindowMenuItem>
    {
        private EntityWindow m_Window;

        protected EntityWindow Window => m_Window;
        protected bool IsFocused => m_Window.IsFocused;

        public virtual string Name => Syadeu.Collections.TypeHelper.ToString(GetType());
        public virtual string Description => string.Empty;

        public abstract int Order { get; }

        public void Initialize(EntityWindow window)
        {
            m_Window = window;
            OnIntialize(window);
        }
        public virtual void OnIntialize(EntityWindow window) { }
        public abstract void OnListGUI(Rect pos);
        public abstract void OnViewGUI(Rect pos);
        public virtual void OnDispose() { }

        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void OnFocus() { }
        public virtual void OnLostFocus() { }
        public virtual void OnSelectionChanged(GameObject[] objects) { }

        void IDisposable.Dispose()
        {
            OnDispose();

            m_Window = null;
        }

        int IComparable<EntityWindowMenuItem>.CompareTo(EntityWindowMenuItem other)
        {
            if (other == null) return -1;
            else if (Order < other.Order) return -1;
            else if (Order > other.Order) return 1;
            else return 0;
        }
    }
}
