namespace Syadeu.Mono.Console
{
    [System.Flags]
    public enum CommandInputType
    {
        None = 0,

        /// <summary>
        /// Requires 프로퍼티의 리턴이 예상값과 일치할 경우에만 자동완성 및 콘솔창에 표시됩니다.
        /// </summary>
        ShowIfRequiresTrue = 1 << 0,

        All = ~0
    }
}
