using System.Collections;

namespace Syadeu.Presentation
{
    public interface ICoroutineJob
    {
        IEnumerator Execute();
    }
}
