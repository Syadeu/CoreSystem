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

namespace Syadeu.Presentation.Proxy
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
            GameObjectSystemExtensions.s_System = this;

            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnDispose()
        {
            m_SceneSystem = null;
            m_ProxySystem = null;

            GameObjectSystemExtensions.s_System = null;
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
            ProxyTransform tr = m_ProxySystem.CreateTransform(0, quaternion.identity, 1);
            int index;

            if (m_ReservedObjects.Count > 0)
            {
                index = m_ReservedObjects.Pop();
                obj = m_GameObjects[index];

                obj.GameObject.SetActive(true);
                obj.Transform = tr;
            }
            else
            {
                GameObject temp = CreateGameObject(string.Empty, true);
                index = temp.GetInstanceID();
#if UNITY_EDITOR
                temp.name = index.ToString();
#endif
                obj = new GameObjectHandler(temp, tr);
                m_GameObjects.Add(index, obj);
            }

            m_ProxySystem.ConnectTransform(in tr, obj.GameObject.transform);
            
            return new FixedGameObject(index, tr);
        }
        public void ReserveGameObject(in FixedGameObject obj)
        {
            GameObjectHandler handler = m_GameObjects[obj.m_Index];
            m_ProxySystem.Destroy(handler.Transform);

            for (int i = 0; i < handler.m_AddedComponents.Count; i++)
            {
                Destroy(handler.m_AddedComponents[i]);
            }

            handler.m_AddedComponents.Clear();

            handler.GameObject.SetActive(false);
            m_ReservedObjects.Push(obj.m_Index);
        }

        internal sealed class GameObjectHandler : IDisposable
        {
            private GameObject m_GameObject;
            private ProxyTransform m_Transform;

            public List<Component> m_AddedComponents;

            public GameObject GameObject => m_GameObject;
            public ProxyTransform Transform
            {
                get => m_Transform;
                set => m_Transform = value;
            }

            public GameObjectHandler(GameObject obj, ProxyTransform tr)
            {
                m_GameObject = obj;
                m_Transform = tr;

                m_AddedComponents = new List<Component>();
            }

            public Component AddComponent(Type t)
            {
                Component component = m_GameObject.AddComponent(t);
                m_AddedComponents.Add(component);

                return component;
            }
            public Component GetComponent(Type t)
            {
                return m_GameObject.GetComponent(t);
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
    public static class GameObjectSystemExtensions
    {
        internal static GameObjectSystem s_System;

        private static GameObjectSystem.GameObjectHandler GetHandler(in FixedGameObject t)
        {
            return s_System.m_GameObjects[t.m_Index];
        }

        public static GameObject GetTarget(this FixedGameObject t)
        {
            return GetHandler(t).GameObject;
        }
        public static int GetInstanceID(this FixedGameObject t)
        {
            return GetHandler(t).GameObject.GetInstanceID();
        }
        public static void SetLayer(this FixedGameObject t, int layer)
        {
            GetHandler(t).GameObject.layer = layer;
        }
        public static void Reserve(this FixedGameObject t)
        {
            s_System.ReserveGameObject(t);
        }

        public static TComponent AddComponent<TComponent>(this FixedGameObject t)
            where TComponent : UnityEngine.Component
        {
            return (TComponent)GetHandler(t).AddComponent(TypeHelper.TypeOf<TComponent>.Type);
        }
        public static TComponent GetComponent<TComponent>(this FixedGameObject t)
            where TComponent : UnityEngine.Component
        {
            return (TComponent)GetHandler(t).GetComponent(TypeHelper.TypeOf<TComponent>.Type);
        }
        public static TComponent GetOrAddComponent<TComponent>(this FixedGameObject t)
            where TComponent : UnityEngine.Component
        {
            var handler = GetHandler(t);
            Component component = handler.GetComponent(TypeHelper.TypeOf<TComponent>.Type);
            if (component == null)
            {
                component = handler.AddComponent(TypeHelper.TypeOf<TComponent>.Type);
            }

            return (TComponent)component;
        }
    }

    [BurstCompatible]
    public struct FixedGameObject : IEquatable<FixedGameObject>, IValidation, IEmpty
    {
        public static FixedGameObject Null => new FixedGameObject(-1, ProxyTransform.Null);

        internal readonly int m_Index;

        public readonly ProxyTransform transform;

        internal FixedGameObject(int index, ProxyTransform transform)
        {
            m_Index = index;
            this.transform = transform;
        }

        [NotBurstCompatible]
        public static FixedGameObject CreateInstance()
        {
            return PresentationSystem<DefaultPresentationGroup, GameObjectSystem>.System.GetGameObject();
        }

        public bool IsValid() => !IsEmpty() && !transform.isDestroyed;
        public bool Equals(FixedGameObject other)
        {
            return transform.Equals(other.transform) && m_Index.Equals(other.m_Index);
        }

        public bool IsEmpty() => m_Index == -1 && transform.Equals(ProxyTransform.Null);
    }
}
