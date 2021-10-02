using System;

namespace Syadeu.Presentation.TurnTable
{
    [Obsolete("", true)]
    public interface ITurnPlayer : ITurnObject, IEquatable<ITurnPlayer>
    {
        int ActionPoint { get; }
    }
}

