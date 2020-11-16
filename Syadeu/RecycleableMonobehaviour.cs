using UnityEngine;

namespace Syadeu
{
    /// <summary>
    /// 재사용 가능 오브젝트들의 기본 참조 클래스입니다<br/>
    /// Awake, Start 함수를 절때 사용하지마세요 대신 OnInitialize를 사용하세요
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RecycleableMonobehaviour : MonoBehaviour
    {
        /// <summary>
        /// 이 오브젝트의 인스턴스 인덱스입니다.
        /// </summary>
        public int IngameIndex { get; internal set; }
        public bool Activated { get; internal set; } = false;

        /// <summary>
        /// GetObject() 함수를 호출했을때 재사용을 위해 실행되는 초기화 함수입니다.<br/>
        /// Unity 의 Awake 함수랑 비슷하다고 보면됨
        /// </summary>
        public virtual void OnInitialize() { }
        public virtual void OnTerminate() { }
        public void Terminate()
        {
            OnTerminate();
            Activated = false;
        }
    }
}
