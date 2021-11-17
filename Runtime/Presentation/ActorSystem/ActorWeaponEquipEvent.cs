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
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public struct ActorWeaponEquipEvent : IActorWeaponEquipEvent
    {
        private ActorWeaponEquipOptions m_EquipOptions;
        private Instance<ActorWeaponData> m_Weapon;

        public ActorWeaponEquipOptions EquipOptions => m_EquipOptions;
        public Instance<ActorWeaponData> Weapon => m_Weapon;

        public bool BurstCompile => false;

        public ActorWeaponEquipEvent(ActorWeaponEquipOptions options, Instance<ActorWeaponData> weapon)
        {
            m_EquipOptions = options;
            m_Weapon = weapon;
        }
        public ActorWeaponEquipEvent(ActorWeaponEquipOptions options, Reference<ActorWeaponData> weapon)
        {
            m_EquipOptions = options;
            m_Weapon = weapon.CreateInstance();

            $"weapon {m_Weapon.IsValid()} : {m_Weapon.GetObject().Name}".ToLog();
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
        }
    }
}
