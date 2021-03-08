using System;
#if UNITY_EDITOR
#endif

namespace Syadeu.ECS
{
    public abstract class ECSModule : IDisposable
    {
        public bool Disposed { get; private set; } = false;

        public virtual void Dispose()
        {
            Disposed = true;
        }
    }
}
