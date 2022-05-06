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

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Presentation.Proxy
{
    public class MaskableGraphicFader : MonoBehaviour, IPresentationReceiver
    {
        [SerializeField]
        private MaskableGraphic m_Target;
        [SerializeField]
        private Color m_Color;
        [SerializeField]
        private float m_Speed = 10;

        private Color m_Original;

        public void OnCreated()
        {
            m_Original = m_Target.color;
            m_Target.color = m_Color;
        }
        public void OnIntialize(Entity<IEntity> entity)
        {
            //var temp = m_Target.color;
            //temp.a = 0;
            m_Target.color = m_Color;

            StartCoroutine(Updater(m_Original));
        }
        public void OnTerminate(Entity<IEntity> entity)
        {
            m_Target.color = m_Original;
            //var temp = m_Target.color;
            //temp.a = 0;

            StartCoroutine(Updater(m_Color));
        }

        private IEnumerator Updater(Color target)
        {
            while (!Mathf.Approximately(m_Target.color.a, target.a))
            {
                m_Target.color = Color.Lerp(m_Target.color, target, Time.deltaTime * m_Speed);

                yield return null;
            }

            var temp = m_Target.color;
            temp.a = target.a;
            m_Target.color = temp;
        }
    }
}
