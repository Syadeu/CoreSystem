namespace Syadeu.Collections
{
    /// <summary>
    /// 값을 체크하는 방식을 설정합니다.
    /// </summary>
    public enum ObValueDetection
    {
        /// <summary>
        /// 어떤값이 들어와도 항상 체크합니다.
        /// </summary>
        Constant,
        /// <summary>
        /// 값이 이전값이랑 다를때만 체크합니다.
        /// </summary>
        Changed
    }
}
