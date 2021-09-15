using Newtonsoft.Json;
using Syadeu.Presentation.Data;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Weapon")]
    public class ActorWeaponData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "WeaponType")]
        private Reference<ActorWeaponTypeData> m_WeaponType = Reference<ActorWeaponTypeData>.Empty;

        [Space, Header("General")]
        [JsonProperty(Order = 1, PropertyName = "Damage")] private float m_Damage;

        [JsonIgnore] public Reference<ActorWeaponTypeData> WeaponType => m_WeaponType;
        [JsonIgnore] public float Damage
        {
            get
            {
                return m_Damage;
            }
        }
    }
}
