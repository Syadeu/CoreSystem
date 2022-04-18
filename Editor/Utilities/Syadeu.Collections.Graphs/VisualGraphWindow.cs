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
using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Graphs;
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SyadeuEditor.Utilities
{
    public class VisualGraphWindow : BaseGraphWindow
    {
        BaseGraph tmpGraph;

        //[MenuItem("Window/01 test")]
        //public static VisualGraphWindow Open()
        //{
        //    var window = GetWindow<VisualGraphWindow>();

        //    window.tmpGraph = ScriptableObject.CreateInstance<BaseGraph>();
        //    window.tmpGraph.hideFlags = HideFlags.HideAndDontSave;
        //    window.InitializeGraph(window.tmpGraph);

        //    window.Show();

        //    return window;
        //}
        protected override void OnDestroy() 
        {
            graphView?.Dispose();
            DestroyImmediate(tmpGraph);
        }
        protected override void InitializeWindow(BaseGraph graph)
        {
            // Set the window title
            titleContent = new GUIContent("Default Graph");

            // Here you can use the default BaseGraphView or a custom one (see section below)
            var graphView = new VisualGraphView(this);

            GridBackground background = new GridBackground();
            graphView.Insert(0 , background);
            background.StretchToParentSize();

            graphView.Add(new MiniMapView(graphView));

            rootView.Add(graphView);
        }
    }
    public class VisualGraphView : BaseGraphView
    {
        public VisualGraphView(EditorWindow window) : base(window)
        {
        }

        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();

            evt.menu.AppendAction("To Json",
                e =>
                {
                    //Debug.Log($"{JsonConvert.SerializeObject(this.graph)}");
                    //string json = JsonConvert.SerializeObject(new FloatNode());
                    string json = JsonConvert.SerializeObject(graph);

                    Debug.Log(json);

                    VisualGraph temp = JsonConvert.DeserializeObject<VisualGraph>(json);
                },
                status: DropdownMenuAction.Status.Normal);
            evt.menu.AppendSeparator();

            BuildStackNodeContextualMenu(evt);

            base.BuildContextualMenu(evt);
        }
        /// <summary>
        /// Add the New Stack entry to the context menu
        /// </summary>
        /// <param name="evt"></param>
        protected void BuildStackNodeContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 position = (evt.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
            evt.menu.AppendAction("Create Stack", (e) => AddStackNode(new BaseStackNode(position)), DropdownMenuAction.AlwaysEnabled);
        }
    }
}
