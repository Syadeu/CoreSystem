using System;
using UnityEngine;

namespace Syadeu.Mono
{
    public abstract class DataComponent : IDisposable
    {
        protected bool Initialized { get; private set; } = false;
        private bool Disposed { get; set; } = false;

        private DataBehaviour m_Parent;

        public DataBehaviour Parent
        {
            get => m_Parent;
            set
            {
                if (m_Parent == value) return;
                if (value != m_Parent)
                {
                    if (m_Parent != null)
                    {
                        m_Parent.RemoveDataComponent(this);
                    }
                }

                m_Parent = value;
                if (value != null) m_Parent.DataComponents.Add(this);
            }
        }

        internal void Initialize()
        {
            if (Disposed || Initialized) return;

            Awake();
            Start();

            Initialized = true;
        }

        protected virtual void Awake() { }
        protected virtual void Start() { }

        public T GetComponent<T>() where T : Behaviour
        {
            T component = Parent.GetComponent<T>();

#if UNITY_EDITOR
            if (component == null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"{typeof(T).Name} 을 찾을 수 없습니다.");
            }
#endif
            return component;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
