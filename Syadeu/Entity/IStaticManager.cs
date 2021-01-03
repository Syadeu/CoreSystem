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
        /// 인스턴트가 생성될때 한번 실행할 함수입니다.
        /// </summary>
        void OnInitialize();
        /// <summary>
        /// 초기화 함수입니다.
        /// </summary>
        void Initialize(SystemFlag flag = SystemFlag.SubSystem);
    }
}
