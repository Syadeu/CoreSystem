using System;
using System.Collections;

namespace Syadeu.Presentation
{
    public interface ICoroutineJob : IDisposable
    {
        UpdateLoop Loop { get; }
        IEnumerator Execute();
    }
}
