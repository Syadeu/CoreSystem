using UnityEngine;

namespace Syadeu.Mono
{
    public abstract class CreatureEntity : MonoBehaviour, IInitialize<CreatureBrain, int>, IRender
    {
        [SerializeField] private CreatureBrain m_Brain = null;
        [SerializeField] private int m_DataIdx = -1;

        public CreatureBrain Brain => m_Brain;
        public int DataIdx => m_DataIdx;

        public bool Initialized { get; private set; } = false;

        public void Initialize(CreatureBrain t, int ta)
        {
            m_Brain = t;
            m_DataIdx = ta;
            
            OnInitialize(t, ta);
            Initialized = true;

            $"{name}. {GetType().Name}: {ta} : init done".ToLog();
        }
        internal void InternalOnTerminate()
        {
            m_DataIdx = -1;

            OnTerminate();
            Initialized = false;
        }
        internal void InternalOnCreated() => OnCreated();

        protected virtual void OnCreated() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="brain">이 크리쳐의 메인 스크립트</param>
        /// <param name="dataIdx">이 크리쳐의 데이터 인덱스</param>
        protected virtual void OnInitialize(CreatureBrain brain, int dataIdx) { }
        protected virtual void OnTerminate() { }

        public virtual void OnVisible() { }
        public virtual void OnInvisible() { }
    }
}
