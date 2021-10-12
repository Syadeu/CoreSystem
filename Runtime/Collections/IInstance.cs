using Syadeu.Collections;
using System;

namespace Syadeu.Collections
{
    public interface IInstance : IValidation, IEquatable<IInstance>
    {
        Hash Idx { get; }
        IObject Object { get; }
    }
}
