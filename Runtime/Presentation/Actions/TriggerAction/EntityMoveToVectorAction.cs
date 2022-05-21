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
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System.Collections;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Move to Vector3")]
    public sealed class EntityMoveToVectorAction : TriggerAction
    {
        public enum UpdateType
        {
            Instant =   0,
            Lerp    =   1
        }

        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "Target")] 
        private float3 m_Target;
        [UnityEngine.SerializeField, JsonProperty(Order = 1, PropertyName = "UpdateType")] 
        private UpdateType m_UpdateType;
        [UnityEngine.SerializeField, JsonProperty(Order = 2, PropertyName = "Speed")] 
        private float m_Speed = 5;

        protected override void OnExecute(Entity<IObject> e)
        {
            if (!(e.Target is EntityBase entity))
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation,
                    "Target is not a EntityBase");
                return;
            }

            switch (m_UpdateType)
            {
                case UpdateType.Lerp:
                    CoreSystem.StartUnityUpdate(this, Lerp(entity.GetTransform(), m_Target, m_Speed));
                    break;
                default:
                    entity.GetTransform().position = m_Target;
                    break;
            }
        }

        private static IEnumerator Lerp(ITransform tr, float3 pos, float speed)
        {
            while (sqr(tr.position - pos) > .1f)
            {
                tr.position = math.lerp(tr.position, pos, Time.deltaTime * speed);
                yield return null;
            }

            tr.position = pos;
        }

        private static float sqr(float3 translation)
        {
            return math.mul(translation, translation);
        }
    }
}
