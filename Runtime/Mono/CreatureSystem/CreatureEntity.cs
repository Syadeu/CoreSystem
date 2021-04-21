using UnityEngine;

namespace Syadeu.Mono
{
    public abstract class CreatureEntity : MonoBehaviour, IInitialize<CreatureBrain, int>
    {
        private CreatureBrain m_Brain = null;
        private int m_DataIdx = -1;

        public CreatureBrain Brain => m_Brain;
        public int DataIdx => m_DataIdx;

        public bool Initialized { get; private set; } = false;

        public void Initialize(CreatureBrain t, int ta)
        {
            m_Brain = t;
            m_DataIdx = ta;

            OnInitialize(t, ta);
            Initialized = true;
        }
        internal void InternalOnTerminate()
        {
            m_DataIdx = -1;

            OnTerminate();
            Initialized = false;
        }
        internal void InternalOnCreated() => OnCreated();

        protected virtual void OnCreated() { }
        protected virtual void OnInitialize(CreatureBrain brain, int dataIdx) { }
        protected virtual void OnTerminate() { }
    }
}
