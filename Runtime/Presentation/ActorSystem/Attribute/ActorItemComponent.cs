// Copyright 2022 Seung Ha Kim
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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public struct ActorItemComponent : IEntityComponent, IDisposable
    {
        public struct WeaponPosition
        {
            public bool m_UseBone;
            public HumanBodyBones m_AttachedBone;

            public float3 m_WeaponPosOffset;
            public float3 m_WeaponRotOffset;
        }

        private Reference<ActorItemType> m_ItemType;
        //private UnsafeLinkedBlock m_ItemSpace;

        private float m_Damage;
        internal WeaponPosition m_HolsterPosition;
        internal WeaponPosition m_DrawPosition;

        public Reference<ActorItemType> ItemType => m_ItemType;
        //public UnsafeLinkedBlock ItemSpace => m_ItemSpace;

        public float Damage { get => m_Damage; set => m_Damage = value; }

        public ActorItemComponent(ActorItemAttribute att)
        {
            m_ItemType = att.ItemType;
            //m_ItemSpace = new UnsafeLinkedBlock(att.GeneralInfo.ItemSpace, Unity.Collections.Allocator.Persistent);

            m_Damage = att.WeaponInfo.m_Damage;
            m_HolsterPosition = new WeaponPosition
            {
                m_AttachedBone = att.WeaponInfo.m_HolsterPosition.m_AttachedBone,
                m_UseBone = att.WeaponInfo.m_HolsterPosition.m_UseBone,
                m_WeaponPosOffset = att.WeaponInfo.m_HolsterPosition.m_WeaponPosOffset,
                m_WeaponRotOffset = att.WeaponInfo.m_HolsterPosition.m_WeaponRotOffset
            };
            m_DrawPosition = new WeaponPosition
            {
                m_AttachedBone = att.WeaponInfo.m_DrawPosition.m_AttachedBone,
                m_UseBone = att.WeaponInfo.m_DrawPosition.m_UseBone,
                m_WeaponPosOffset = att.WeaponInfo.m_DrawPosition.m_WeaponPosOffset,
                m_WeaponRotOffset = att.WeaponInfo.m_DrawPosition.m_WeaponRotOffset
            };
        }
        public void Dispose()
        {
            //m_ItemSpace.Dispose();
        }
    }

}
