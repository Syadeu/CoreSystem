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
using Syadeu;
using Syadeu.Collections;
using Syadeu.Collections.Graphs;
using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Syadeu.Collections.Graphs.Editor
{
    public static class VisualGraphAssetCallbacks
    {
        [MenuItem("Assets/CoreSystem/Graphs/Visual Graph", false, 0)]
        public static void CreateGraphPorcessor()
        {
            CreateGraphAsset<VisualGraph>(true);
        }

        public static VisualGraph CreateGraphAsset(Type type, bool save)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                CoreHelper.LogError(LogChannel.Editor,
                    $"Cannot create asset({TypeHelper.ToString(type)}) is abstract.");
                return null;
            }

            VisualGraph graph = (VisualGraph)ScriptableObject.CreateInstance(type);
            if (save)
            {
                ProjectWindowUtil.CreateAsset(graph, $"{TypeHelper.ToString(type)}.asset");
            }

            string paramGuid = graph.AddExposedParameter("This", TypeHelper.TypeOf<SystemObjectExposedParameter>.Type);
            var param = graph.GetExposedParameter("This");

            //if (graph is VisualLogicGraph logicGraph)
            //{
            //    EntryNode entryNode = new EntryNode();
            //    entryNode.OnNodeCreated();

            //    logicGraph.AddNode(entryNode);

            //    //var param = logicGraph.GetExposedParameterFromGUID(paramGuid);
            //}

            return graph;
        }
        public static bool OpenGraphAsset(VisualGraph graph)
        {
            if (graph == null) return false;

            //else if (graph is VisualLogicGraph)
            //{
            //    EditorWindow.GetWindow<VisualLogicGraphWindow>().InitializeGraph(graph);
            //    return true;
            //}

            EditorWindow.GetWindow<VisualGraphWindow>().InitializeGraph(graph);
            return true;
        }

        public static T CreateGraphAsset<T>(bool save) where T : VisualGraph => (T)CreateGraphAsset(TypeHelper.TypeOf<T>.Type, save);

        [OnOpenAsset(0)]
        public static bool OnBaseGraphOpened(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as VisualGraph;
            if (asset == null) return false;

            return OpenGraphAsset(asset);
        }
    }
}
