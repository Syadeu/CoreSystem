using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Weapon")]
    public class ActorWeaponData : DataObjectBase
    {
        public enum OverrideOptions
        {
            None,

            Override,
            Addictive
        }

        [JsonProperty(Order = 0, PropertyName = "WeaponType")]
        protected Reference<ActorWeaponTypeData> m_WeaponType = Reference<ActorWeaponTypeData>.Empty;
        [JsonProperty(Order = 1, PropertyName = "Prefab")]
        protected Reference<ObjectEntity> m_Prefab = Reference<ObjectEntity>.Empty;

        [Space, Header("General")]
        [JsonProperty(Order = 2, PropertyName = "Damage")] protected float m_Damage;

        [Space, Header("Weapon Position")]
        [JsonProperty(Order = 3, PropertyName = "OverrideOptions")]
        protected OverrideOptions m_OverrideOptions = OverrideOptions.None;
        [JsonProperty(Order = 4, PropertyName = "AttachedBone")]
        protected HumanBodyBones m_AttachedBone = HumanBodyBones.RightHand;
        [JsonProperty(Order = 5, PropertyName = "WeaponPosOffset")]
        protected float3 m_WeaponPosOffset = float3.zero;
        [JsonProperty(Order = 6, PropertyName = "WeaponRotOffset")]
        protected float3 m_WeaponRotOffset = float3.zero;

        [JsonIgnore] private Entity<ObjectEntity> m_PrefabInstance = Entity<ObjectEntity>.Empty;

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
            if (!m_Prefab.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"{nameof(ActorWeaponData)}({Name}) has an invalid entity. This is not allowed.");
                return;
            }

            Instance<ObjectEntity> instance = m_Prefab.CreateInstance();
            m_PrefabInstance = Entity<ObjectEntity>.GetEntityWithoutCheck(instance.Idx);

            "weapon created".ToLog();
        }
        protected override void OnDestroy()
        {
            m_PrefabInstance.Destroy();
        }
    }
}
