namespace Syadeu
{
    public enum SystemFlag
    {
        MainSystem,
        SubSystem,

        Data,
    }
    public interface IStaticManager
    {
        /// <summary>
        /// true 일 경우, 씬이 전환되어도 파괴되지 않습니다.
        /// </summary>
        bool DontDestroy { get; }
        /// <summary>
        /// 인스턴트가 생성될때 한번 실행할 함수입니다.
        /// </summary>
        void OnInitialize();
        /// <summary>
        /// 초기화 함수입니다.
        /// </summary>
        void Initialize(SystemFlag flag = SystemFlag.SubSystem);
    }
}
