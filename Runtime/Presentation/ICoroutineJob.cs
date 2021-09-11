using System;
using System.Collections;

namespace Syadeu.Presentation
{
    public interface ICoroutineJob : IDisposable
    {
        IEnumerator Execute();
    }
}
