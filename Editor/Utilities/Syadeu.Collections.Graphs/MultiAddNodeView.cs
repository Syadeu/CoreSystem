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
using UnityEditor.UIElements;

namespace SyadeuEditor.Utilities
{
    [NodeCustomEditor(typeof(MultiAddNode))]
    public class MultiAddNodeView : BaseNodeView
    {
        public override void Enable()
        {
            var floatNode = nodeTarget as MultiAddNode;

            DoubleField floatField = new DoubleField
            {
                value = floatNode.output
            };

            // Update the UI value after each processing
            nodeTarget.onProcessed += () => floatField.value = floatNode.output;

            controlsContainer.Add(floatField);
        }
    }
}
