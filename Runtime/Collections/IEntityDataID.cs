using System;
using Unity.Collections;

namespace Syadeu.Collections
{
    public interface IEntityDataID : IValidation, IEmpty, IEquatable<IEntityDataID>
    {
        FixedString128Bytes RawName { get; }
        string Name { get; }
        Hash Hash { get; }
        EntityID Idx { get; }
        Type Type { get; }

        IEntityData Target { get; }

        int GetHashCode();
    }
}
