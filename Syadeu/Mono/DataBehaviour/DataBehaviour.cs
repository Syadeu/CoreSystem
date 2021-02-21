using System;
using System.Collections.Generic;

using UnityEngine;

namespace Syadeu.Mono
{
    public abstract class DataBehaviour : MonoBehaviour
    {
        internal List<DataComponent> DataComponents { get; set; } = new List<DataComponent>();

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < DataComponents.Count; i++)
            {
                DataComponents[i].Dispose();
            }
            DataComponents.Clear();
        }

        public T AddDataComponent<T>() where T : DataComponent
        {
            T component = Activator.CreateInstance<T>();

            component.Parent = this;
            DataComponents.Add(component);

            return component;
        }
        public void RemoveDataComponent<T>(T component) where T : DataComponent
        {
            for (int i = 0; i < DataComponents.Count; i++)
            {
                if (DataComponents[i] == component)
                {
                    DataComponents.RemoveAt(i);
                    break;
                }
            }
        }
        public void RemoveDataComponent(DataComponent component)
        {
            for (int i = 0; i < DataComponents.Count; i++)
            {
                if (DataComponents[i] == component)
                {
                    DataComponents.RemoveAt(i);
                    break;
                }
            }
        }
        public T GetDataComponent<T>() where T : DataComponent
        {
            for (int i = 0; i < DataComponents.Count; i++)
            {
                if (DataComponents[i].GetType() == typeof(T))
                {
                    return (T)DataComponents[i];
                }
            }

            throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                $"{typeof(T).Name} 을 찾을 수 없습니다.");
        }
    }
}
