using System;

namespace Syadeu
{
    /// <summary>
    /// 객체 사용자 생성 매니저 기본 클래스입니다.
    /// </summary>
    [Obsolete("퇴역됩니다 MonoManager<T>를 사용하세요", true)]
    public abstract class MonoManagerEntity : ManagerEntity
    {
        [Obsolete("퇴역됩니다")]
        public bool IsReady { get; protected set; }
    }
}
