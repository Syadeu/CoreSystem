using Syadeu.Database;

namespace Syadeu.Mono
{
    public interface IHash
    {
        Hash Hash { get; }
    }
    public interface IObject : IHash
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

