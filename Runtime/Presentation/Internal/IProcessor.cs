using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Internal
{
    [RequireImplementors]
    internal interface IProcessor : IDisposable
    {
        /// <summary>
        /// 이 프로세서가 타겟으로 삼을 <see cref="ObjectBase"/>입니다.
        /// </summary>
        Type Target { get; }

        void OnInitialize();
        void OnInitializeAsync();
    }
}
