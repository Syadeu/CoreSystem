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
    }

    public class ActorWeaponData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "WeaponType")]
        private Reference<ActorWeaponTypeData> m_WeaponType = Reference<ActorWeaponTypeData>.Empty;

        [Space, Header("General")]
        [JsonProperty(Order = 1, PropertyName = "Damage")] private float m_Damage;
    }
    public class ActorWeaponTypeData : DataObjectBase
    {

    }
}
