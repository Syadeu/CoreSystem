using Syadeu.Database;
using System;
using UnityEngine;

namespace Syadeu.Mono
{
    [Serializable]
    public abstract class DataComponent : IDisposable, IValidation
    {
        private DataBehaviour m_Parent;

        public bool Disposed { get; private set; } = false;
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
                if (value != null) m_Parent.m_DataComponents.Add(this);
            }
        }

        //internal DataComponent()
        //{
        //    Awake();
        //    Start();
        //}

        internal void InternalAwake() => Awake();
        internal void InternalStart() => Start();

        protected virtual void Awake() { }
        protected virtual void Start() { }

        public T GetComponent<T>() where T : Behaviour
        {
            if (Parent == null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"부모가 없는 데이터 컴포넌트에서는 GetComponent를 사용할 수 없습니다.");
            }

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
        public bool IsValid() => !Disposed;
    }
}
