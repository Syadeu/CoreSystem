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

using Syadeu.Presentation.Proxy;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    public class CanvasRendererModule<TRenderer> : PresentationSystemModule<ICanvasSystem>
        where TRenderer : Renderer
    {
        private Stack<FixedGameObject> m_ReservedRenderers;
        private Dictionary<int, FixedGameObject> m_UsedRenderers;

        private GameObjectSystem m_GameObjectSystem;

        protected override void OnInitialize()
        {
            m_ReservedRenderers = new Stack<FixedGameObject>();
            m_UsedRenderers = new Dictionary<int, FixedGameObject>();

            RequestSystem<DefaultPresentationGroup, GameObjectSystem>(Bind);
        }
        protected override void OnShutDown()
        {
            foreach (var item in m_ReservedRenderers)
            {
                m_GameObjectSystem.ReserveGameObject(item);
            }
        }
        protected override void OnDispose()
        {
            m_GameObjectSystem = null;
        }

        private void Bind(GameObjectSystem other)
        {
            m_GameObjectSystem = other;
        }

        public TRenderer GetRenderer()
        {
            FixedGameObject obj;
            TRenderer renderer;
            if (m_ReservedRenderers.Count == 0)
            {
                obj = m_GameObjectSystem.GetGameObject();
                renderer = obj.AddComponent<TRenderer>();
            }
            else
            {
                obj = m_ReservedRenderers.Pop();
                renderer = obj.GetComponent<TRenderer>();
            }
            m_UsedRenderers.Add(obj.GetInstanceID(), obj);

            return renderer;
        }
        public void ReserveRenderer(TRenderer renderer)
        {
            GameObject obj = renderer.gameObject;
            int idx = obj.GetInstanceID();

            if (!m_UsedRenderers.ContainsKey(idx))
            {
                CoreSystem.Logger.LogError(Channel.Proxy, "");
                return;
            }

            FixedGameObject temp = m_UsedRenderers[idx];
            m_UsedRenderers.Remove(idx);

            m_ReservedRenderers.Push(temp);
        }
    }
}
