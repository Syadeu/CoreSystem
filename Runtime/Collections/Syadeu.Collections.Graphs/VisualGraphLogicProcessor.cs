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

using GraphProcessor;
using System.Linq;

namespace Syadeu.Collections.Graphs
{
    /// <summary>
    /// require <see cref="EntryNode"/>
    /// </summary>
    public class VisualGraphLogicProcessor : BaseGraphProcessor
    {
        private EntryNode[] m_EntryNodes;

        public VisualGraphLogicProcessor() : base(null) { }
        public VisualGraphLogicProcessor(VisualGraph graph) : base(graph)
        {
        }
        public void Initialize(VisualGraphField graph)
        {
            this.graph = graph.VisualGraph;
        }

        public override void Run()
        {
            for (int i = 0; i < m_EntryNodes.Length; i++)
            {
                BaseNode current = m_EntryNodes[i];
                Execute(current);
            }
        }
        public override void UpdateComputeOrder()
        {
            m_EntryNodes = graph.nodes.Where(t => t is EntryNode).Select(t => (EntryNode)t).ToArray();
        }

        private void Execute(BaseNode current)
        {
            current.OnProcess();

            foreach (var outputNode in current.GetOutputNodes())
            {
                Execute(outputNode);
            }
        }
    }
    public static class VisualGraphLogicProcessorExtensions
    {
        public static void Process(this VisualGraph t)
        {
            VisualGraphLogicProcessor p = new VisualGraphLogicProcessor(t);
            p.UpdateComputeOrder();
            p.Run();
        }
        public static void Process(this VisualGraphField t)
        {
            Process(t.VisualGraph);
        }
    }
}
