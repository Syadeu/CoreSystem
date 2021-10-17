using Newtonsoft.Json;
using System;

namespace Syadeu.Collections
{
    [Serializable]
    public abstract class PropertyBlockBase
    {
        [JsonProperty(Order = -100, PropertyName = "Name")]
        protected string m_Name;
        [JsonProperty(Order = -99, PropertyName = "UseInstance")]
        protected bool m_UseInstance;

        internal abstract PropertyBlockBase InternalGetProperty();
    }
    public abstract class PropertyBlock<T> : PropertyBlockBase
        where T : PropertyBlockBase, new()
    {
        internal override PropertyBlockBase InternalGetProperty()
        {
            return GetProperty();
        }
        public T GetProperty()
        {
            if (m_UseInstance)
            {
                T t = new T();
                OnCreateInstance(t);
                return t;
            }
            else
            {
                return this as T;
            }
        }
        protected virtual void OnCreateInstance(T instance) { }

    }
}
