using Newtonsoft.Json;
using Syadeu.Database;
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
        [JsonProperty(Order = 1, PropertyName = "Prefab")]
        protected PrefabReference<GameObject> m_Prefab = PrefabReference<GameObject>.None;

        [Space, Header("General")]
        [JsonProperty(Order = 2, PropertyName = "Damage")] private float m_Damage;

        [JsonIgnore] public Reference<ActorWeaponTypeData> WeaponType => m_WeaponType;
        [JsonIgnore] public float Damage
        {
            get
            {
                return m_Damage;
            }
        }

        protected override void OnCreated()
        {
            if (m_Prefab.IsNone() || !m_Prefab.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(ActorWeaponData)}({Name}) has an invalid prefab. This is not allowed.");
                return;
            }


        }
    }
}
