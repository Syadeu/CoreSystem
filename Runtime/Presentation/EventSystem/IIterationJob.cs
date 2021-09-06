using System.Collections;

namespace Syadeu.Presentation.Events
{
    public interface IIterationJob
    {
        IEnumerator Execute();
    }
}
