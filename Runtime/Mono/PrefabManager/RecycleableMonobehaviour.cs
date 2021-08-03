using Syadeu.Database;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Syadeu.Mono
{
    /// <summary>
    /// 재사용 가능 오브젝트들의 기본 참조 클래스입니다<br/>
    /// Awake, Start 함수를 절때 사용하지마세요 대신 OnInitialize를 사용하세요
    /// OnDestroy 함수를 절때 사용하지마세요
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RecycleableMonobehaviour : MonoBehaviour
    {
        public delegate bool TerminateCondition();
        /// <summary>
        /// <see cref="Presentation.GameObjectProxySystem.m_Instances"/> value 리스트의 인덱스입니다.
        /// </summary>
        internal int m_Idx = -1;

        /// <summary>
        /// PrefabManager 인스펙터창에서 보여질 이름입니다.
        /// 런타임에 아무런 영향을 주지않습니다.
        /// </summary>
        public virtual string DisplayName => name;
        public virtual bool InitializeOnCall => true;

        /// <summary>
        /// 이 모노 프록시 객체가 <see cref="Presentation.GameObjectProxySystem"/>에서 사용 중인지 반환합니다.
        /// </summary>
        public bool Activated { get; private set; } = false;
        [Obsolete] public bool WaitForDeletion { get; internal set; } = false;

        public virtual void Initialize()
        {
            if (Activated) throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject,
                "이미 초기화 된 재사용 오브젝트를 또 초기화하려합니다.");

            OnInitialize();
            //gameObject.SetActive(true);
            Activated = true;
        }

        /// <summary>
        /// 이 객체가 생성되었을때만 한번 실행하는 함수입니다.
        /// </summary>
        protected virtual void OnCreated() { }
        internal void InternalOnCreated() => OnCreated();
        /// <summary>
        /// <see cref="Presentation.GameObjectProxySystem"/>에서 이 프록시 모노 객체를 재사용을 위해 실행되는 초기화 함수입니다.
        /// </summary>
        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }

        /// <summary>
        /// False를 반환시키면 이 모노객체는 즉시 <see cref="Terminate"/>됩니다.
        /// </summary>
        [Obsolete] public TerminateCondition OnActivated;

        internal void Terminate()
        {
            OnTerminate();
            Activated = false;
        }
    }
}
