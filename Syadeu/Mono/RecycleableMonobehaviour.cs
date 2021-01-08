using UnityEngine;

namespace Syadeu.Mono
{
    /// <summary>
    /// 재사용 가능 오브젝트들의 기본 참조 클래스입니다<br/>
    /// Awake, Start 함수를 절때 사용하지마세요 대신 OnInitialize를 사용하세요
    /// OnDestroy 함수를 절때 사용하지마세요
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RecycleableMonobehaviour : MonoBehaviour, IRecycleable
    {
        public delegate bool TerminateCondition();

        /// <summary>
        /// PrefabManager 인스펙터창에서 보여질 이름입니다.
        /// 런타임에 아무런 영향을 주지않습니다.
        /// </summary>
        public virtual string DisplayName => "None";

        public bool Activated { get; internal set; } = false;
        public bool WaitForDeletion { get; internal set; } = false;
        
        public abstract Transform Transfrom { get; }

        /// <summary>
        /// 이 객체가 생성되었을때만 한번 실행하는 함수입니다.
        /// </summary>
        public virtual void OnCreated() { }
        /// <summary>
        /// GetObject() 함수를 호출했을때 재사용을 위해 실행되는 초기화 함수입니다.<br/>
        /// Unity 의 OnEnable 함수랑 비슷하다고 보면됨
        /// </summary>
        public virtual void OnInitialize() { }
        public virtual void OnTerminate() { }

        /// <summary>
        /// False를 반환시키면 이 모노객체는 즉시 <see cref="Terminate"/>됩니다.
        /// </summary>
        public TerminateCondition OnActivated;

        public void Terminate()
        {
            OnTerminate();
            Activated = false;
        }
    }
}
