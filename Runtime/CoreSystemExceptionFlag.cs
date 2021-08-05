namespace Syadeu
{
    public enum CoreSystemExceptionFlag
    {
        /// <summary>
        /// 에디터에서 발생한 예외사항입니다.
        /// </summary>
        Editor,

        /// <summary>
        /// 잡을 실행하는 도중 발생한 예외사항입니다.
        /// </summary>
        Jobs,
        /// <summary>
        /// ECS에서 발생한 예외사항입니다.
        /// </summary>
        ECS,

        /// <summary>
        /// 백그라운드 스레드에서 발생한 예외사항입니다.
        /// </summary>
        Background,
        /// <summary>
        /// 메인 유니티 스레드에서 발생한 예외사항입니다.
        /// </summary>
        Foreground,

        /// <summary>
        /// 재사용 오브젝트에서 발생한 예외사항입니다.
        /// </summary>
        RecycleObject,
        /// <summary>
        /// 랜더 매니저에서 발생한 예외사항입니다.
        /// </summary>
        Render,
        /// <summary>
        /// 콘솔에서 발생한 예외사항입니다.
        /// </summary>
        Console,
        /// <summary>
        /// 데이터관련 메소드에서 예외사항입니다.
        /// </summary>
        Database,
        /// <summary>
        /// 모노 기반 오브젝트에서 발생한 예외사항입니다.
        /// </summary>
        Mono,

        Presentation,
        Proxy,
    }
}
