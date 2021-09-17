using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
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
        public readonly struct OverrideData
        {
            private readonly Instance<ActorWeaponData> m_Instance;

            public OverrideOptions OverrideOptions => m_Instance.Object.m_OverrideOptions;
            public bool UseBone => m_Instance.Object.m_UseBone;
            public HumanBodyBones AttachedBone => m_Instance.Object.m_AttachedBone;
            public float3 WeaponPosOffset => m_Instance.Object.m_WeaponPosOffset;
            public float3 WeaponRotOffset => m_Instance.Object.m_WeaponRotOffset;

            public OverrideData(ActorWeaponData data)
            {
                m_Instance = new Instance<ActorWeaponData>(data.Idx);
            }
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
        [JsonProperty(Order = 4, PropertyName = "UseBone")]
        protected bool m_UseBone = true;
        [JsonProperty(Order = 5, PropertyName = "AttachedBone")]
        protected HumanBodyBones m_AttachedBone = HumanBodyBones.RightHand;
        [JsonProperty(Order = 6, PropertyName = "WeaponPosOffset")]
        protected float3 m_WeaponPosOffset = float3.zero;
        [JsonProperty(Order = 7, PropertyName = "WeaponRotOffset")]
        protected float3 m_WeaponRotOffset = float3.zero;

        [Space, Header("FX")]
        [JsonProperty(Order = 8, PropertyName = "FXBounds")]
        protected FXBounds[] m_FXBounds = Array.Empty<FXBounds>();

        [JsonIgnore] private Entity<ObjectEntity> m_PrefabInstance = Entity<ObjectEntity>.Empty;

        [JsonIgnore] public Entity<ObjectEntity> PrefabInstance => m_PrefabInstance;
        [JsonIgnore] public OverrideData Overrides => new OverrideData(this);
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
            if (!m_Prefab.IsEmpty())
            {
                if (!m_Prefab.IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"{nameof(ActorWeaponData)}({Name}) has an invalid entity. This is not allowed.");
                    return;
                }

                Instance<ObjectEntity> instance = m_Prefab.CreateInstance();
                m_PrefabInstance = Entity<ObjectEntity>.GetEntityWithoutCheck(instance.Idx);

                $"weapon({Name}, {m_Prefab.GetObject().Name}) created".ToLog();
            }

            FireFXBounds((FXBounds.TriggerOptions)~0);
        }
        protected override void OnDestroy()
        {
            m_PrefabInstance.Destroy();
        }

        public void FireFXBounds(FXBounds.TriggerOptions triggerOptions)
        {
            if (m_PrefabInstance.IsEmpty() || !m_PrefabInstance.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"Cannot fire FX({TypeHelper.Enum<FXBounds.TriggerOptions>.ToString(triggerOptions)}), target entity({m_Prefab.GetObject().Name}) in {Name} has been destroyed or invalid.");
                return;
            }

            for (int i = 0; i < m_FXBounds.Length; i++)
            {
                if ((m_FXBounds[i].TriggerOption & triggerOptions) == 0) continue;

                var ins = m_FXBounds[i].FXEntity.CreateInstance();
                Entity<FXEntity> fx = Entity<FXEntity>.GetEntityWithoutCheck(ins.Idx);

                TRS prefabTRS = new TRS(m_PrefabInstance.transform),
                    trs = m_FXBounds[i].TRS.Project(prefabTRS);

                ITransform tr = fx.transform;
                tr.position = trs.m_Position;
                tr.rotation = trs.m_Rotation;
                tr.scale = trs.m_Scale;

                $"{m_FXBounds[i].FXEntity.GetObject().Name} fired".ToLog();
            }
        }
    }
}
