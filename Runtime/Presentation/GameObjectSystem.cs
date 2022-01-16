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
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class GameObjectSystem : PresentationSystemEntity<GameObjectSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly Stack<int> m_ReservedObjects = new Stack<int>();
        internal readonly Dictionary<int, GameObjectHandler> m_GameObjects = new Dictionary<int, GameObjectHandler>();

        private SceneSystem m_SceneSystem;
        private GameObjectProxySystem m_ProxySystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnDispose()
        {
            m_SceneSystem = null;
            m_ProxySystem = null;
        }

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
        }
        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;
        }

        public FixedGameObject GetGameObject()
        {
            GameObjectHandler obj;
            int index;
            if (m_ReservedObjects.Count > 0)
            {
                index = m_ReservedObjects.Pop();
                obj = m_GameObjects[index];

                obj.m_GameObject.SetActive(true);
            }
            else
            {
                GameObject temp = CreateGameObject(string.Empty, true);
                index = temp.GetInstanceID();
#if UNITY_EDITOR
                temp.name = index.ToString();
#endif
                obj = new GameObjectHandler()
                {
                    m_GameObject = temp,
                    m_AddedComponents = new List<Component>()
                };
                m_GameObjects.Add(index, obj);
            }

            ProxyTransform tr = m_ProxySystem.CreateTransform(0, quaternion.identity, 1);
            m_ProxySystem.ConnectTransform(in tr, obj.m_GameObject.transform);
            obj.m_Transform = tr;

            return new FixedGameObject(index, tr);
        }
        public void ReserveGameObject(FixedGameObject obj)
        {
            GameObjectHandler handler = m_GameObjects[obj.m_Index];
            m_ProxySystem.Destroy(handler.m_Transform);

            for (int i = 0; i < handler.m_AddedComponents.Count; i++)
            {
                Destroy(handler.m_AddedComponents[i]);
            }

            handler.m_AddedComponents.Clear();

            handler.m_GameObject.SetActive(false);
            m_ReservedObjects.Push(obj.m_Index);
        }

        internal sealed class GameObjectHandler
        {
            public GameObject m_GameObject;
            public ProxyTransform m_Transform;

            public List<Component> m_AddedComponents;
        }
    }

    [BurstCompatible]
    public struct FixedGameObject : IDisposable
    {
        internal readonly int m_Index;
        
        [NotBurstCompatible]
        public GameObject Target
        {
            get
            {

                return PresentationSystem<DefaultPresentationGroup, GameObjectSystem>.System.m_GameObjects[m_Index].m_GameObject;
            }
        }

        public readonly ProxyTransform transform;

        internal FixedGameObject(int index, ProxyTransform transform)
        {
            m_Index = index;
            this.transform = transform;
        }

        [NotBurstCompatible]
        public void Dispose()
        {
            PresentationSystem<DefaultPresentationGroup, GameObjectSystem>.System.ReserveGameObject(this);
        }
        [NotBurstCompatible]
        public static FixedGameObject CreateInstance()
        {
            return PresentationSystem<DefaultPresentationGroup, GameObjectSystem>.System.GetGameObject();
        }
    }
}
