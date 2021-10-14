using Syadeu.Collections;
using System;

namespace Syadeu.Collections
{
    public interface IInstance : IValidation, IEmpty, IEquatable<IInstance>
    {
        Hash Idx { get; }
        IObject Object { get; }
    }
    public interface IInstance<T> : IInstance
        where T : class, IObject
    {
        new T Object { get; }
    }
}
