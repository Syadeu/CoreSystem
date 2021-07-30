using System;

namespace Syadeu.Presentation.Internal
{
    internal interface IProcessor
    {
        /// <summary>
        /// 이 프로세서가 타겟으로 삼을 <see cref="ObjectBase"/>입니다.
        /// </summary>
        Type Target { get; }
    }
}
