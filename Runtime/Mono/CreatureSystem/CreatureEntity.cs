using Syadeu.Database;
using Syadeu.Presentation;
using System;
using UnityEngine;

namespace Syadeu.Mono
{
    public abstract class CreatureEntity : MonoBehaviour
    {
        private CreatureBrain m_Brain = null;
        private DataGameObject m_DataObject;

        public CreatureBrain Brain => m_Brain;

        public bool Initialized { get; private set; } = false;

        internal void InternalInitialize(CreatureBrain t, DataGameObject obj)
        {
            m_Brain = t;
            m_DataObject = obj;

            OnInitialize(t, obj);
            Initialized = true;

            //$"{name}. {GetType().Name}: {ta} : init done".ToLog();
        }
        internal void InternalOnTerminate()
        {
            m_Brain = null;
            //m_DataHash = Hash.Empty;

            OnTerminate();
            Initialized = false;
        }
        internal void InternalOnCreated() => OnCreated();
        internal void InternalOnStart() => OnStart();

        protected virtual void OnCreated() { }

        protected virtual void OnInitialize(CreatureBrain brain, DataGameObject obj) { }
        protected virtual void OnStart() { }
        protected virtual void OnTerminate() { }

        //public virtual void OnVisible() { }
        //public virtual void OnInvisible() { }
    }
}
