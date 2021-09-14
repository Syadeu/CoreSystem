using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    public interface IInstance : IValidation, IEquatable<IInstance>
    {
        Hash Idx { get; }
        IObject Object { get; }
    }
}
