using System;
using System.Collections.Generic;

using UnityEngine;

namespace Syadeu.Mono
{
    public abstract class DataBehaviour : MonoBehaviour
    {
        internal List<DataComponent> m_DataComponents = new List<DataComponent>();

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < m_DataComponents.Count; i++)
            {
                m_DataComponents[i].Dispose();
            }
            m_DataComponents.Clear();
        }

        public T AddDataComponent<T>() where T : DataComponent
        {
            T component = Activator.CreateInstance<T>();

            component.Parent = this;
            m_DataComponents.Add(component);

            component.InternalAwake();
            component.InternalStart();

            return component;
        }
        public void RemoveDataComponent<T>(T component) where T : DataComponent
        {
            for (int i = 0; i < m_DataComponents.Count; i++)
            {
                if (m_DataComponents[i] == component)
                {
                    m_DataComponents.RemoveAt(i);
                    break;
                }
            }
        }
        public void RemoveDataComponent(DataComponent component)
        {
            for (int i = 0; i < m_DataComponents.Count; i++)
            {
                if (m_DataComponents[i] == component)
                {
                    m_DataComponents.RemoveAt(i);
                    break;
                }
            }
        }
        public T GetDataComponent<T>() where T : DataComponent
        {
            for (int i = 0; i < m_DataComponents.Count; i++)
            {
                if (m_DataComponents[i].GetType() == typeof(T))
                {
                    return (T)m_DataComponents[i];
                }
            }

            throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                $"{typeof(T).Name} 을 찾을 수 없습니다.");
        }
    }
}
