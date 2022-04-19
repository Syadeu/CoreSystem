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
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [RequireComponent(typeof(RecycleableMonobehaviour))]
    public class ActorItemWeaponMonobehaviour : PresentationBehaviour, IPresentationReceiver
    {
        [Serializable]
        public sealed class FireProperty /*: PropertyBlock<FireProperty>*/
        {
            [SerializeField, PositionHandle(RotationField = nameof(m_FireRotation))]
            private Vector3 m_FirePosition;
            [SerializeField, RotationHandle(PositionField = nameof(m_FirePosition))]
            private Vector4 m_FireRotation = new Vector4(0, 0, 0, 1);

            [Space]
            [SerializeField]
            private Reference<FXEntity> m_MuzzleFlash = Reference<FXEntity>.Empty;

            public void Initialize()
            {
            }
            public void Reserve()
            {
            }

            public void Fire(ActorItemWeaponMonobehaviour weapon)
            {
                var fx = m_MuzzleFlash.CreateEntity(m_FirePosition);
            }
        }

        [SerializeField]
        private FireProperty m_FireProperty = new FireProperty();

        public void OnCreated()
        {
        }
        public void OnIntialize()
        {
        }
        public void OnTerminate()
        {
        }
    }
}
