﻿using Syadeu.Database;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public interface IActorEvent
    {
        ActorEventID EventID { get; }

        void OnExecute(Entity<ActorEntity> from);
    }
}
