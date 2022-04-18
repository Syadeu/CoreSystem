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
using Syadeu.Collections.Graphs;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public static class VisualGraphAssetCallbacks
    {
        [MenuItem("Assets/CoreSystem/Graphs/Visual Graph", false, 0)]
        public static void CreateGraphPorcessor()
        {
            CreateGraphAsset<VisualGraph>();
        }

        private static T CreateGraphAsset<T>() where T : VisualGraph
        {
            T graph = ScriptableObject.CreateInstance<T>();
            ProjectWindowUtil.CreateAsset(graph, $"{TypeHelper.TypeOf<T>.ToString()}.asset");

            return graph;
        }

        [OnOpenAsset(0)]
        public static bool OnBaseGraphOpened(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as BaseGraph;

            //if (asset != null && AssetDatabase.GetAssetPath(asset).Contains("Examples"))
            if (asset != null && asset is VisualGraph baseGraph)
            {
                EditorWindow.GetWindow<VisualGraphWindow>().InitializeGraph(baseGraph);
                return true;
            }
            return false;
        }
    }
}
