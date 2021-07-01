using Syadeu.Database;
using System;

namespace Syadeu.Mono
{
    public interface IHash : IEquatable<Hash>
    {
        Hash Hash { get; }
    }
    public interface IObject : IHash, IEquatable<IObject>
    {
        string DisplayName { get; }
    }
    public interface IPlayer : IObject
    {

    }
    public interface IEnvironment : IObject
    {

    }
}

