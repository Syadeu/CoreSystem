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
using Syadeu.Collections;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SyadeuEditor.Utilities
{
    public class VisualGraphWindow : BaseGraphWindow
    {
        protected override void OnDestroy() 
        {
            graphView?.Dispose();
        }
        protected override void InitializeWindow(BaseGraph graph)
        {
            // Set the window title
            titleContent = new GUIContent("Visual Logic Graph");

            // Here you can use the default BaseGraphView or a custom one (see section below)
            var visualGraphView = new VisualGraphView(this);

            GridBackground background = new GridBackground();
            visualGraphView.Insert(0, background);
            background.StretchToParentSize();

            SetupExposedParameter(visualGraphView, graph);
            //SetupMinimap(visualGraphView);

            rootView.Add(visualGraphView);
            graphView = visualGraphView;
        }

        protected void SetupExposedParameter(VisualGraphView graphView, BaseGraph graph)
        {
            ExposedParameterView view = new ExposedParameterView();
            view.title = "Exposed Properties";

            for (int i = 0; i < graph.exposedParameters.Count; i++)
            {
                ExposedParameter parameter = graph.exposedParameters[i];
                ExposedParameterFieldView field = new ExposedParameterFieldView(graphView, parameter);
                
                view.Add(field);
            }

            graphView.Add(view);
        }
        protected void SetupMinimap(VisualGraphView graphView)
        {
            MiniMapView minimap = new MiniMapView(graphView);
            //Vector2 cords = graphView.contentViewContainer.WorldToLocal(new Vector2(10, 30));
            //minimap.SetPosition(new Rect(cords.x, cords.y, 200, 300));
            graphView.Add(minimap);
        }
    }
}
