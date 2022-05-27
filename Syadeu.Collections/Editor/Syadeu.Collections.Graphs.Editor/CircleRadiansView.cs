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

using GraphProcessor;
using Syadeu.Collections.Graphs;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Syadeu.Collections.Graphs.Editor
{
    [NodeCustomEditor(typeof(CircleRadians))]
    public class CircleRadiansView : BaseNodeView
    {
        CircleRadians node;
        VisualElement listContainer;

        public override void Enable()
        {
            node = nodeTarget as CircleRadians;

            listContainer = new VisualElement();
            // Create your fields using node's variables and add them to the controlsContainer

            controlsContainer.Add(listContainer);
            onPortConnected += OnPortUpdate;
            onPortDisconnected += OnPortUpdate;

            UpdateOutputRadians(GetFirstPortViewFromFieldName("outputRadians").connectionCount);
        }

        void UpdateOutputRadians(int count)
        {
            node.outputRadians = new List<float>();

            listContainer.Clear();

            for (int i = 0; i < count; i++)
            {
                float r = (Mathf.PI * 2 / count) * i;
                node.outputRadians.Add(r);
                listContainer.Add(new Label(r.ToString("F3")));
            }
        }

        public void OnPortUpdate(PortView port)
        {
            // There is only one port on this node so it can only be the output
            UpdateOutputRadians(port.connectionCount);
        }
    }
}
