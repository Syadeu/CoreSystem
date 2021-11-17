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

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;

namespace Syadeu.Presentation.Actor
{
    public struct ActorWeaponComponent : IEntityComponent, IDisposable
    {
        internal Entity<ActorEntity> m_Parent;
        //internal Instance<ActorWeaponProvider> m_Provider;
        //internal CoroutineJob m_WeaponPoser;

        internal int m_MaxEquipableCount;
        internal Reference<ActorWeaponData> m_DefaultWeapon;
        //internal Instance<ActorWeaponData> m_DefaultWeaponInstance;
        internal FixedInstanceList16<ActorWeaponData> m_EquipedWeapons;

        internal FixedReferenceList16<ActorWeaponData> m_ExcludeWeapon;
        internal FixedReferenceList16<ActorWeaponData> m_IncludeWeapon;
        internal FixedReferenceList16<ActorWeaponTypeData> m_ExcludeWeaponType;
        internal FixedReferenceList16<ActorWeaponTypeData> m_IncludeWeaponType;

        internal FixedReferenceList16<TriggerAction> m_OnWeaponSelected;
        internal FixedReferenceList16<TriggerAction> m_OnEquipWeapon;
        internal FixedReferenceList16<TriggerAction> m_OnUnequipWeapon;

        internal int m_SelectedWeaponIndex;
        internal bool m_WeaponHolster;
        internal bool m_WeaponAiming;

        //public ActorWeaponProvider Provider => m_Provider.GetObject();
        public FixedInstanceList16<ActorWeaponData> EquipedWeapons => m_EquipedWeapons;
        public Instance<ActorWeaponData> SelectedWeapon
        {
            get
            {
                if (m_EquipedWeapons.Length == 0 || m_SelectedWeaponIndex >= m_EquipedWeapons.Length)
                {
                    return Instance<ActorWeaponData>.Empty;
                }

                return m_EquipedWeapons[m_SelectedWeaponIndex];
            }
        }
        public int Selected => m_SelectedWeaponIndex;
        public int Equiped => m_EquipedWeapons.Length;
        public bool Holster
        {
            get => m_WeaponHolster;
            set => m_WeaponHolster = value;
        }
        public bool Aiming
        {
            get => Holster ? false : m_WeaponAiming;
            set => m_WeaponAiming = value;
        }
        public float WeaponDamage
        {
            get
            {
                if (EquipedWeapons[m_SelectedWeaponIndex].IsEmpty())
                {
                    if (m_DefaultWeapon.IsEmpty()) return 0;
                    else if (!m_DefaultWeapon.IsValid())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"Entity({m_Parent.Name}) has an invalid default weapon.");
                        return 0;
                    }

                    return m_DefaultWeapon.GetObject().Damage;
                }

                return EquipedWeapons[m_SelectedWeaponIndex].GetObject().Damage;
            }
        }

        public void SelectWeapon(int index)
        {
            //ActorWeaponProvider provider = Provider;
            if (index < 0 || index >= m_MaxEquipableCount)
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"{nameof(SelectWeapon)} index out of range. Index {index}.");
                return;
            }

            m_SelectedWeaponIndex = index;
            m_OnWeaponSelected.Execute(m_Parent.As<ActorEntity, IEntityData>());
        }
        public bool IsEquipable(Instance<ActorWeaponData> weapon)
        {
            var original = weapon.AsOriginal();
            var weaponObj = weapon.GetObject();

            //ActorWeaponProvider provider = Provider;

            if (m_ExcludeWeaponType.Contains(weaponObj.WeaponType))
            {
                if (!m_IncludeWeapon.Contains(original)) return false;
            }
            else if (m_IncludeWeaponType.Contains(weaponObj.WeaponType))
            {
                if (m_ExcludeWeapon.Contains(original)) return false;
            }

            return true;
        }

        void IDisposable.Dispose()
        {
            for (int i = 0; i < m_EquipedWeapons.Length; i++)
            {
                m_EquipedWeapons[i].Destroy();
            }
            m_EquipedWeapons.Clear();
        }
    }
}
