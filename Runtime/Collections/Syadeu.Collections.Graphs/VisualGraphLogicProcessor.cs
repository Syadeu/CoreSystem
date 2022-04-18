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
