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
using UnityEngine.AI;

namespace Syadeu.Presentation.Map
{
    public sealed class NavMeshBaker : MonoBehaviour
    {
        internal bool m_Registered = false;
        internal NavMeshData m_NavMeshData;
        internal NavMeshDataInstance m_Handle;

        [SerializeField] internal int m_AgentType = 0;
        [SerializeField] private Vector3 m_Center = Vector3.zero;
        [SerializeField] private Vector3 m_Size = Vector3.one;

        internal Bounds Bounds => new Bounds(m_Center, m_Size);

        private void Awake()
        {
            m_NavMeshData = new NavMeshData();
        }
        private void OnEnable()
        {
            CoreSystem.StartUnityUpdate(this, Authoring(true));
        }
        private void OnDisable()
        {
            if (!CoreSystem.BlockCreateInstance)
            {
                PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System.RemoveBaker(this);
            }
        }

        private IEnumerator Authoring(bool enable)
        {
            yield return PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.GetAwaiter();

            if (enable)
            {
                PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System.AddBaker(this);
            }
            else PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System.RemoveBaker(this);
        }
    }
}
