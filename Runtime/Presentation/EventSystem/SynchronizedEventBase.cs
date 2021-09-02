using Syadeu.Database;
using System.Collections;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 직접 상속은 허용하지 않습니다. <seealso cref="SynchronizedEvent{TEvent}"/>를 상속하여 사용하세요.
    /// </summary>
    public abstract class SynchronizedEventBase : IValidation
    {
        internal abstract void InternalPost();
        internal abstract void InternalTerminate();

        public virtual bool IsValid() => true;
    }
}
