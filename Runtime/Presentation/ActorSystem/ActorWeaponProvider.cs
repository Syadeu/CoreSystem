using Newtonsoft.Json;
using System;
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
                }

                return EquipedWeapon.Object.Damage;
            }
        }

        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (!(ev is IActorWeaponEquipEvent weaponEquipEvent)) return;

            m_EquipedWeapon = weaponEquipEvent.Weapon;
        }
    }
}
