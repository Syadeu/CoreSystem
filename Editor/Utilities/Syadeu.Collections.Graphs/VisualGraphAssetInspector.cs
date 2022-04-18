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
using UnityEditor;
using UnityEngine.UIElements;

namespace SyadeuEditor.Utilities
{
    [CustomEditor(typeof(VisualGraph), true)]
    public class VisualGraphAssetInspector : GraphInspector
    {
        // protected override void CreateInspector()
        // {
        // }

        protected override void CreateInspector()
        {
            base.CreateInspector();

            root.Add(new Button(() => EditorWindow.GetWindow<VisualGraphWindow>().InitializeGraph(target as BaseGraph))
            {
                text = "Open base graph window"
            });
            //root.Add(new Button(() => EditorWindow.GetWindow<CustomContextMenuGraphWindow>().InitializeGraph(target as BaseGraph))
            //{
            //    text = "Open custom context menu graph window"
            //});
            //root.Add(new Button(() => EditorWindow.GetWindow<CustomToolbarGraphWindow>().InitializeGraph(target as BaseGraph))
            //{
            //    text = "Open custom toolbar graph window"
            //});
            //root.Add(new Button(() => EditorWindow.GetWindow<ExposedPropertiesGraphWindow>().InitializeGraph(target as BaseGraph))
            //{
            //    text = "Open exposed properties graph window"
            //});
        }
    }
}
