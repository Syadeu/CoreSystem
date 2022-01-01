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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
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
            Override,
            Addictive
        }
        public readonly struct OverrideData
        {
            private readonly Entity<ActorWeaponData> m_Instance;

            public OverrideOptions HolsterOverrideOptions => m_Instance.Target.m_HolsterPosition.m_OverrideOptions;
            public bool HolsterUseBone => m_Instance.Target.m_HolsterPosition.m_UseBone;
            public HumanBodyBones HolsterAttachedBone => m_Instance.Target.m_HolsterPosition.m_AttachedBone;
            public float3 HolsterWeaponPosOffset => m_Instance.Target.m_HolsterPosition.m_WeaponPosOffset;
            public float3 HolsterWeaponRotOffset => m_Instance.Target.m_HolsterPosition.m_WeaponRotOffset;

            public OverrideOptions DrawOverrideOptions => m_Instance.Target.m_DrawPosition.m_OverrideOptions;
            public bool DrawUseBone => m_Instance.Target.m_DrawPosition.m_UseBone;
            public HumanBodyBones DrawAttachedBone => m_Instance.Target.m_DrawPosition.m_AttachedBone;
            public float3 DrawWeaponPosOffset => m_Instance.Target.m_DrawPosition.m_WeaponPosOffset;
            public float3 DrawWeaponRotOffset => m_Instance.Target.m_DrawPosition.m_WeaponRotOffset;

            public OverrideData(ActorWeaponData data)
            {
                m_Instance = Entity<ActorWeaponData>.GetEntity(data.Idx);
            }
        }
        public sealed class WeaponPositionProperty : PropertyBlock<WeaponPositionProperty>
        {
            [JsonProperty(Order = 0, PropertyName = "OverrideOptions")]
            public OverrideOptions m_OverrideOptions = OverrideOptions.Override;
            [JsonProperty(Order = 1, PropertyName = "UseBone")]
            public bool m_UseBone = true;
            [JsonProperty(Order = 2, PropertyName = "AttachedBone")]
            public HumanBodyBones m_AttachedBone = HumanBodyBones.RightHand;
            [JsonProperty(Order = 3, PropertyName = "WeaponPosOffset")]
            public float3 m_WeaponPosOffset = float3.zero;
            [JsonProperty(Order = 4, PropertyName = "WeaponRotOffset")]
            public float3 m_WeaponRotOffset = float3.zero;
        }

        [JsonProperty(Order = 0, PropertyName = "WeaponType")]
        protected Reference<ActorWeaponTypeData> m_WeaponType = Reference<ActorWeaponTypeData>.Empty;
        [JsonProperty(Order = 1, PropertyName = "Prefab")]
        protected Reference<ObjectEntity> m_Prefab = Reference<ObjectEntity>.Empty;

        [Space, Header("General")]
        [JsonProperty(Order = 2, PropertyName = "Damage")] protected float m_Damage;
        [JsonProperty(Order = 3, PropertyName = "Range")] protected float m_Range;

        [Space, Header("Weapon Position")]
        [JsonProperty(Order = 4, PropertyName = "HolsterPosition")]
        protected WeaponPositionProperty m_HolsterPosition = new WeaponPositionProperty();
        [JsonProperty(Order = 5, PropertyName = "DrawPosition")]
        protected WeaponPositionProperty m_DrawPosition = new WeaponPositionProperty();

        [Space, Header("FX")]
        [JsonProperty(Order = 9, PropertyName = "FXBounds")]
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
        [JsonIgnore] public float Range => m_Range;

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

                m_PrefabInstance = m_Prefab.CreateEntity(float3.zero);

                $"weapon({Name}, {m_Prefab.GetObject().Name}) created".ToLog();
            }

            m_HolsterPosition = m_HolsterPosition.GetProperty();
            m_DrawPosition = m_DrawPosition.GetProperty();
            //FireFXBounds((FXBounds.TriggerOptions)~0);
        }
        protected override void OnDestroy()
        {
            if (m_PrefabInstance.IsValid())
            {
                m_PrefabInstance.Destroy();
            }
        }

        public void FireFXBounds(ITransform sender, FXBounds.TriggerOptions triggerOptions)
        {
            CoroutineSystem system = PresentationSystem<DefaultPresentationGroup, CoroutineSystem>.System;
            FireFXBounds(sender, system, triggerOptions);
        }
        public void FireFXBounds(ITransform sender,
            CoroutineSystem coroutineSystem, FXBounds.TriggerOptions triggerOptions)
        {
            ITransform targetTr;
            if (m_PrefabInstance.IsEmpty())
            {
                targetTr = sender;
            }
            else if (!m_PrefabInstance.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity, 
                    $"Cannot fire FX({TypeHelper.Enum<FXBounds.TriggerOptions>.ToString(triggerOptions)}), " +
                    $"target prefab in {Name} is invalid.");
                return;
            }
            else
            {
                targetTr = m_PrefabInstance.transform;
            }

            for (int i = 0; i < m_FXBounds.Length; i++)
            {
                if ((m_FXBounds[i].TriggerOption & triggerOptions) == 0) continue;

                m_FXBounds[i].Fire(coroutineSystem, targetTr);
            }
        }
    }
}
