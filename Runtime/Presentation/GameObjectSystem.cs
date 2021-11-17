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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class GameObjectSystem : PresentationSystemEntity<GameObjectSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly Stack<int> m_ReservedObjects = new Stack<int>();
        internal readonly Dictionary<int, GameObject> m_GameObjects = new Dictionary<int, GameObject>();

        private SceneSystem m_SceneSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_SceneSystem = null;
        }

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
        }

        public FixedGameObject GetGameObject()
        {
            if (m_ReservedObjects.Count > 0)
            {
                return new FixedGameObject(m_ReservedObjects.Pop());
            }

            GameObject obj = CreateGameObject(string.Empty, true);
            int idx = obj.GetInstanceID();
#if UNITY_EDITOR
            obj.name = idx.ToString();
#endif
            m_GameObjects.Add(idx, obj);

            return new FixedGameObject(idx);
        }
        public void ReserveGameObject(FixedGameObject obj)
        {
            m_ReservedObjects.Push(obj.m_Index);
        }
    }

    public struct FixedGameObject : IDisposable
    {
        internal readonly int m_Index;

        public GameObject Target
        {
            get
            {

                return PresentationSystem<DefaultPresentationGroup, GameObjectSystem>.System.m_GameObjects[m_Index];
            }
        }

        internal FixedGameObject(int index)
        {
            m_Index = index;
        }

        public void Dispose()
        {
            PresentationSystem<DefaultPresentationGroup, GameObjectSystem>.System.ReserveGameObject(this);
        }

        public static FixedGameObject CreateInstance()
        {
            return PresentationSystem<DefaultPresentationGroup, GameObjectSystem>.System.GetGameObject();
        }
    }
}
