using Syadeu.Database;
using System;
using UnityEngine;

namespace Syadeu.Mono
{
    public abstract class CreatureEntity : MonoBehaviour, IRender
    {
        private CreatureBrain m_Brain = null;
        [Obsolete] private int m_DataIdx = -1;
        public Hash m_DataHash;

        public CreatureBrain Brain => m_Brain;
        public int DataIdx => m_DataIdx;

        public bool Initialized { get; private set; } = false;

        public bool IsVisible => m_Brain.IsVisible;

        internal void InternalInitialize(CreatureBrain t, int ta, Hash dataHash)
        {
            m_Brain = t;
            m_DataIdx = ta;
            m_DataHash = dataHash;

            OnInitialize(t, ta);
            OnInitialize(t, dataHash);
            Initialized = true;

            //$"{name}. {GetType().Name}: {ta} : init done".ToLog();
        }
        internal void InternalOnTerminate()
        {
            m_Brain = null;
            m_DataIdx = -1;

            OnTerminate();
            Initialized = false;
        }
        internal void InternalOnCreated() => OnCreated();
        internal void InternalOnStart() => OnStart();

        protected virtual void OnCreated() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="brain">이 크리쳐의 메인 스크립트</param>
        /// <param name="dataIdx">이 크리쳐의 데이터 인덱스<br/>
        /// <paramref name="dataIdx"/> == <see cref="CreatureBrain.m_DataIdx"/></param>
        [Obsolete] protected virtual void OnInitialize(CreatureBrain brain, int dataIdx) { }
        protected virtual void OnInitialize(CreatureBrain brain, Hash dataHash) { }
        protected virtual void OnStart() { }
        protected virtual void OnTerminate() { }

        public virtual void OnVisible() { }
        public virtual void OnInvisible() { }
    }
}
