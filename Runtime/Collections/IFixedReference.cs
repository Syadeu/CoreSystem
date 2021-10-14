using System;

namespace Syadeu.Collections
{
    public interface IFixedReference : IValidation, IEmpty, IEquatable<IFixedReference>
    {
        Hash Hash { get; }
    }
    public interface IFixedReference<T> : IFixedReference
        where T : class, IObject
    { }
}
