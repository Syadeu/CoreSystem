using UnityEditor;

namespace SyadeuEditor.Tree
{
    public sealed class VerticalLabelTreeElement : VerticalTreeElement
    {
        private string m_Label1 = null;
        private string m_Label2 = null;

        public VerticalLabelTreeElement(VerticalTreeView tree, string label) : base(tree)
        {
            m_Label1 = label;
        }
        public VerticalLabelTreeElement(VerticalTreeView tree, string label1, string label2) : base(tree)
        {
            m_Label1 = label1; m_Label2 = label2;
        }

        public override void OnGUI() => EditorGUILayout.LabelField(m_Label1, m_Label2);
    }
}