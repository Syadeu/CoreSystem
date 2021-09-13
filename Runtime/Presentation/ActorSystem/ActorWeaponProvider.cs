using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public class ActorWeaponProvider : ActorProviderBase
    {
        [Header("Accept Weapon Types")]
        [JsonProperty(Order = 0, PropertyName = "ExcludeWeapon")]
        private Reference<ActorWeaponData>[] m_ExcludeWeapon = Array.Empty<Reference<ActorWeaponData>>();
        [JsonProperty(Order = 1, PropertyName = "IncludeWeapon")]
        private Reference<ActorWeaponData>[] m_IncludeWeapon = Array.Empty<Reference<ActorWeaponData>>();
        [JsonProperty(Order = 2, PropertyName = "ExcludeWeaponType")]
        private Reference<ActorWeaponTypeData>[] m_ExcludeWeaponType = Array.Empty<Reference<ActorWeaponTypeData>>();
        [JsonProperty(Order = 3, PropertyName = "IncludeWeaponType")]
        private Reference<ActorWeaponTypeData>[] m_IncludeWeaponType = Array.Empty<Reference<ActorWeaponTypeData>>();

        [Header("General")]
        [JsonProperty(Order = 4, PropertyName = "DefaultWeapon")]
        private Reference<ActorWeaponData> m_DefaultWeapon = Reference<ActorWeaponData>.Empty;

        [JsonIgnore] private Instance<ActorWeaponData> m_EquipedWeapon;

        [JsonIgnore] public Instance<ActorWeaponData> EquipedWeapon => m_EquipedWeapon;
        [JsonIgnore] public float WeaponDamage
        {
            get
            {
                if (EquipedWeapon.IsEmpty())
                {
                    if (m_DefaultWeapon.IsEmpty()) return 0;
                    else if (!m_DefaultWeapon.IsValid())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"Entity({Parent.Name}) has an invalid default weapon.");
                        return 0;
                    }

                    return m_DefaultWeapon.GetObject().Damage;
                }

                return EquipedWeapon.Object.Damage;
            }
        }

        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (!(ev is IActorWeaponEquipEvent weaponEquipEvent)) return;

            if (!IsEquipable(weaponEquipEvent.Weapon))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) trying to equip weapon({weaponEquipEvent.Weapon.Object.Name}) that doesn\'t fit.");
                return;
            }

            m_EquipedWeapon = weaponEquipEvent.Weapon;
            CoreSystem.Logger.Log(Channel.Entity,
                $"Entity({Parent.Name}) has equiped weapon({m_EquipedWeapon.Object.Name}).");
        }

        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            for (int i = 0; i < m_ExcludeWeapon.Length; i++)
            {
                if (m_IncludeWeapon.Contains(m_ExcludeWeapon[i]))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"1");
                }
            }
            for (int i = 0; i < m_ExcludeWeaponType.Length; i++)
            {
                if (m_IncludeWeaponType.Contains(m_ExcludeWeaponType[i]))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"2");
                }
            }
        }

        public bool IsEquipable(Instance<ActorWeaponData> weapon)
        {
            var original = weapon.AsOriginal();
            var weaponObj = weapon.Object;

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
    }
}
