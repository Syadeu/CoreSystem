using System;

namespace Syadeu
{
    public interface IStaticDataManager : IStaticManager, IDisposable
    {
        bool Disposed { get; }
    }
}
