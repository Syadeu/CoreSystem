namespace Syadeu
{
    /// <summary>
    /// 객체 사용자 생성 매니저 기본 클래스입니다.
    /// </summary>
    public abstract class MonoManagerEntity : ManagerEntity
    {
        public bool IsReady { get; protected set; }
    }
}
