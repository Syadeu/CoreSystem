using System;
using System.Collections;
using Unity.Jobs;

namespace Syadeu.Presentation
{
    public interface ICoroutineJob : IDisposable
    {
        UpdateLoop Loop { get; }
        IEnumerator Execute();
    }
}
