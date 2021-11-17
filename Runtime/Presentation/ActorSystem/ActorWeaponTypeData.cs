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

using Newtonsoft.Json;
using Syadeu.Presentation.Data;
using System.ComponentModel;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Weapon Type")]
    public class ActorWeaponTypeData : DataObjectBase
    {
        public enum WeaponType
        {
            Melee,
            Ranged
        }

        [JsonProperty(Order = 0, PropertyName = "WeaponType")]
        protected WeaponType m_WeaponType = WeaponType.Melee;
    }
}
