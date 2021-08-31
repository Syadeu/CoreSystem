using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// 유니티 <see cref="Transform"/>을 랩핑하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 참조: <seealso cref="UnityTransform"/>
    /// </remarks>
    public interface IUnityTransform : ITransform, IDisposable
    {
#pragma warning disable IDE1006 // Naming Styles
        ConvertedEntity entity { get; }
        Transform provider { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
