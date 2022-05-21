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
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;

namespace Syadeu.Presentation.Actor
{
    public struct ActorWeaponComponent : IActorProviderComponent, IDisposable
    {
        internal Entity<ActorEntity> m_Parent;
        //internal Instance<ActorWeaponProvider> m_Provider;
        //internal CoroutineJob m_WeaponPoser;

        internal int m_MaxEquipableCount;
        internal Reference<EntityBase> m_DefaultWeapon;
        //internal Instance<ActorWeaponData> m_DefaultWeaponInstance;
        internal FixedInstanceList16<EntityBase> m_EquipedWeapons;

        //internal FixedReferenceList16<ActorWeaponData> m_ExcludeWeapon;
        //internal FixedReferenceList16<ActorWeaponData> m_IncludeWeapon;
        internal FixedReferenceList16<ActorItemType> m_ExcludeWeaponType;
        //internal FixedReferenceList16<ActorItemType> m_IncludeWeaponType;

        internal FixedReferenceList16<TriggerAction> m_OnWeaponSelected;
        internal FixedReferenceList16<TriggerAction> m_OnEquipWeapon;
        internal FixedReferenceList16<TriggerAction> m_OnUnequipWeapon;

        internal int m_SelectedWeaponIndex;
        internal bool m_WeaponHolster;
        internal bool m_WeaponAiming;

        //public ActorWeaponProvider Provider => m_Provider.GetObject();
        public FixedInstanceList16<EntityBase> EquipedWeapons => m_EquipedWeapons;
        public InstanceID<EntityBase> SelectedWeapon
        {
            get
            {
                if (m_EquipedWeapons.Length == 0 || m_SelectedWeaponIndex >= m_EquipedWeapons.Length)
                {
                    return InstanceID<EntityBase>.Empty;
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
                        CoreSystem.Logger.LogError(LogChannel.Entity,
                            $"Entity({m_Parent.Name}) has an invalid default weapon.");
                        return 0;
                    }

                    //return m_DefaultWeapon.GetObject().GetAttribute<ActorWeaponItemAttribute>().Damage;
                }

                //return ((InstanceID)EquipedWeapons[m_SelectedWeaponIndex])
                //    .GetComponentReadOnly<ActorWeaponItemComponent>().Damage;
                throw new Exception();
            }
        }

        public void SelectWeapon(int index)
        {
            //ActorWeaponProvider provider = Provider;
            if (index < 0 || index >= m_MaxEquipableCount)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity, $"{nameof(SelectWeapon)} index out of range. Index {index}.");
                return;
            }

            m_SelectedWeaponIndex = index;
            m_OnWeaponSelected.Execute(m_Parent.ToEntity<IObject>());
        }
        public bool IsEquipable(Entity<EntityBase> weapon)
        {
            //if (!weapon.HasComponent<ActorWeaponItemComponent>())
            //{
            //    return false;
            //}
            //ActorWeaponItemComponent com = weapon.GetComponentReadOnly<ActorWeaponItemComponent>();

            //var original = weapon.AsOriginal();
            //var weaponObj = weapon.Target;

            //ActorWeaponProvider provider = Provider;

            //if (m_ExcludeWeaponType.Contains(com.ItemType))
            //{
            //    //if (!m_IncludeWeapon.Contains(original)) return false;
            //    return false;
            //}
            //else if (m_IncludeWeaponType.Contains(com.ItemType))
            //{
            //    //if (m_ExcludeWeapon.Contains(original)) return false;
            //    return true;
            //}

            return true;
        }

        void IDisposable.Dispose()
        {
            for (int i = 0; i < m_EquipedWeapons.Length; i++)
            {
                m_EquipedWeapons[i].GetEntity().Destroy();
            }
            m_EquipedWeapons.Clear();
        }
    }
}
