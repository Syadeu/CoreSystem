// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
