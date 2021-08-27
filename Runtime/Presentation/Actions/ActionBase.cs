using Newtonsoft.Json;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public abstract class ActionBase : ObjectBase
    {
        [JsonIgnore] private bool m_Terminated = true;
        [JsonIgnore] public Reference m_Reference;

        public bool Terminated => m_Terminated;

        internal virtual void InternalInitialize()
        {
            m_Terminated = false;
        }
        internal virtual void InternalTerminate()
        {
            m_Terminated = true;
        }

        public override sealed object Clone() => base.Clone();
        public override sealed int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override sealed string ToString() => Name;
        public override sealed bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
