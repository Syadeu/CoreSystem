using System;

namespace Syadeu.Presentation.TurnTable
{
    public interface ITurnPlayer : ITurnObject, IEquatable<ITurnPlayer>
    {
        int ActionPoint { get; }
    }
}

