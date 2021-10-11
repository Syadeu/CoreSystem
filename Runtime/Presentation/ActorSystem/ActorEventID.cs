using Syadeu.Collections;
using System;

namespace Syadeu.Presentation.Actor
{
    public readonly struct ActorEventID : IValidation, IEquatable<ActorEventID>
    {
        public static readonly ActorEventID Empty = new ActorEventID(Hash.Empty);
        private readonly Hash m_Hash;

        private ActorEventID(Hash hash)
        {
            m_Hash = hash;
        }
        public bool Equals(ActorEventID other) => m_Hash.Equals(other.m_Hash);
        public bool IsValid() => Equals(Empty);

        public static ActorEventID CreateID() => new ActorEventID(Hash.NewHash());
    }
}
