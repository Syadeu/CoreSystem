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

using GraphProcessor;
using Syadeu.Presentation.Graphs;
using UnityEngine.UIElements;

namespace SyadeuEditor.Presentation
{
    [NodeCustomEditor(typeof(SendMessageNode))]
    internal sealed class SendMessageNodeView : PresenationNodeView<SendMessageNode>
    {
        public override void Enable()
        {
            DrawDefaultInspector();

            var methodNameField = AddControlField("methodName", 
                label: "Method Name",
                showInputDrawer: false);

            methodNameField.style.width = 200;
        }
    }

    public abstract class PresenationNodeView<TNode> : BaseNodeView
        where TNode : PresentationNode
    {
        protected new TNode nodeTarget => (TNode)base.nodeTarget;
    }
}
