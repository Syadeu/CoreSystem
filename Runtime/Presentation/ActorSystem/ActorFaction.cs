﻿// Copyright 2021 Seung Ha Kim
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

using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Faction")]
    public sealed class ActorFaction : DataObjectBase
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [JsonProperty(Order = 0, PropertyName = "FactionType")]
        internal FactionType m_FactionType = FactionType.Player;

        [JsonProperty(Order = 1, PropertyName = "Allies")]
        internal Reference<ActorFaction>[] m_Allies = Array.Empty<Reference<ActorFaction>>();
        [JsonProperty(Order = 2, PropertyName = "Enemies")]
        internal Reference<ActorFaction>[] m_Enemies = Array.Empty<Reference<ActorFaction>>();
#pragma warning restore IDE0044 // Add readonly modifier

        [JsonIgnore] public FactionType FactionType => m_FactionType;

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Entity<ActorFaction>>();
            AotHelper.EnsureList<Entity<ActorFaction>>();
            AotHelper.EnsureType<Reference<ActorFaction>>();
            AotHelper.EnsureList<Reference<ActorFaction>>();
            AotHelper.EnsureType<ActorFaction>();
            AotHelper.EnsureList<ActorFaction>();
        }
    }
}
