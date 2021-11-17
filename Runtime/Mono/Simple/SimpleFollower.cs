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

using System.Collections;
using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class SimpleFollower : MonoBehaviour
    {
        [SerializeField] private Transform m_Target;
        [SerializeField] private Vector3 m_Offset;
        [SerializeField] private float m_Speed;

        [Space]
        [SerializeField] private int m_UpdateType = 0;
        [SerializeField] private int m_UpdateAt = 0;

        private Coroutine m_UpdateCor = null;
        private Vector3 m_TargetPosition = Vector3.zero;

        public Vector3 Target
        {
            get
            {
                if (m_Target == null)
                {
                    return m_TargetPosition;
                }

                m_TargetPosition = CoreSystem.GetPosition(m_Target);
                return m_TargetPosition;
            }
        }
        public Vector3 Offset => m_Offset;
        public float Speed => m_Speed;

        private void OnEnable()
        {
            m_UpdateCor = StartCoroutine(Updater());
        }
        private IEnumerator Updater()
        {
            WaitUntil targetNotNull = new WaitUntil(() => m_Target != null);
            YieldInstruction updateAt = null;
            if (m_UpdateAt == 1) updateAt = new WaitForEndOfFrame();
            else if (m_UpdateAt == 2) updateAt = new WaitForFixedUpdate();

        Try1:
            yield return targetNotNull;

            while (m_Target != null)
            {
                if (m_UpdateType == 0) transform.position = Target + m_Offset;
                else if (m_UpdateType == 1)
                {
                    transform.position
                        = Vector3.Lerp(transform.position, Target + m_Offset, m_Speed * Time.deltaTime);
                }

                switch (m_UpdateAt)
                {
                    case 0:
                        yield return null;
                        break;
                    default:
                        yield return updateAt;
                        break;
                }
            }

            goto Try1;
        }
        private void OnDisable()
        {
            if (m_UpdateCor != null) StopCoroutine(m_UpdateCor);
            m_UpdateCor = null;
        }

        public void SetTarget(Transform tr) => m_Target = tr;
        public void SetTarget(Vector3 target)
        {
            m_Target = null;
            m_TargetPosition = target;
        }
        public void SetOffset(Vector3 offset) => m_Offset = offset;
        public void SetSpeed(float speed) => m_Speed = speed;

        public Transform GetTarget() => m_Target;
    }
}