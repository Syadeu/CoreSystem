using System;
using System.Collections;

namespace Syadeu.Presentation
{
    public interface ICoroutineJob : IDisposable
    {
        CoroutineLoop Loop { get; }
        IEnumerator Execute();
    }
    public enum CoroutineLoop
    {
        Default,

        /// <summary>
        /// <see cref="UnityEngine.PlayerLoop.PostLateUpdate"/>
        /// </summary>
        Transform
    }
}
