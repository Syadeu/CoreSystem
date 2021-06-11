using UnityEditor;

namespace SyadeuEditor.Tree
{
    public sealed class VerticalFolderTreeElement : VerticalTreeElement
    {
        public string m_Description = null;

        public VerticalFolderTreeElement(VerticalTreeView tree, string name) : base(tree)
        {
            m_Name = name;
        }
        public VerticalFolderTreeElement(VerticalTreeView tree, string name, string description) : base(tree)
        {
            m_Name = name;
            m_Description = description;
        }

        public override object Data => m_Name;

        public override void OnGUI()
        {
            if (!string.IsNullOrEmpty(m_Description)) EditorGUILayout.LabelField(m_Description);
        }
    }
}