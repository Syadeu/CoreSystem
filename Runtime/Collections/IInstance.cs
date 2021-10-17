using Syadeu.Collections;
using System;

namespace Syadeu.Collections
{
    public interface IInstance : IEmpty, IEquatable<IInstance>
    {
        Hash Idx { get; }
    }
    public interface IInstance<T> : IInstance
        where T : class, IObject
    {
    }
}
