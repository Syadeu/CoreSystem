﻿// Copyright 2021 Seung Ha Kim
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
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Proxy;
using UnityEngine;

namespace Syadeu.Presentation.Entities
{
    public struct UIObjectCanvasGroupComponent : IEntityComponent
    {
        internal InstanceID m_Parent;
        public bool m_Enabled;

        private float m_Alpha;

        public float Alpha
        {
            get => m_Alpha;
            set
            {
                m_Alpha = value;
                SetAlpha((ProxyTransform)m_Parent.GetEntity<IEntity>().transform);
            }
        }

        private void SetAlpha(ProxyTransform tr)
        {
            if (!tr.hasProxy) return;

            var cg = tr.proxy.GetComponent<CanvasGroup>();
            cg.alpha = m_Alpha;
        }
    }
}
