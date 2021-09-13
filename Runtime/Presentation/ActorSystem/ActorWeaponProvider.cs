using Newtonsoft.Json;
using Syadeu.Presentation.Data;
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

        [JsonIgnore] private ActorWeaponData m_EquipedWeapon;

        [JsonIgnore] public ActorWeaponData EquipedWeapon => m_EquipedWeapon;
        [JsonIgnore] public float WeaponDamage
        {
            get
            {
                if (EquipedWeapon == null)
                {
                    if (m_DefaultWeapon.IsEmpty()) return 0;
                    else if (!m_DefaultWeapon.IsValid())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"Entity({Parent.Name}) has an invalid default weapon.");
                        return 0;
                    }
                }

                return EquipedWeapon.Damage;
            }
        }
    }

    public class ActorWeaponData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "WeaponType")]
        private Reference<ActorWeaponTypeData> m_WeaponType = Reference<ActorWeaponTypeData>.Empty;

        [Space, Header("General")]
        [JsonProperty(Order = 1, PropertyName = "Damage")] private float m_Damage;

        [JsonIgnore] public float Damage
        {
            get
            {
                return m_Damage;
            }
        }
    }
    public class ActorWeaponTypeData : DataObjectBase
    {

    }
}
