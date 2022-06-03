﻿// Copyright 2022 Seung Ha Kim
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
using UnityEngine.UIElements;

namespace Syadeu.Collections.Graphs.Editor
{
    [NodeCustomEditor(typeof(FloatNode))]
    public class FloatNodeView : BaseNodeView
    {
        public override void Enable()
        {
            var floatNode = nodeTarget as FloatNode;

            DoubleField floatField = new DoubleField
            {
                value = floatNode.input
            };

            floatNode.onProcessed += () => floatField.value = floatNode.input;

            floatField.RegisterValueChangedCallback((v) => {
                owner.RegisterCompleteObjectUndo("Updated floatNode input");
                floatNode.input = (float)v.newValue;
            });

            controlsContainer.Add(floatField);
        }
    }
}