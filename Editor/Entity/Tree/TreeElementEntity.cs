using System.Collections.Generic;
using UnityEngine;

namespace SyadeuEditor.Tree
{
    public abstract class TreeElementEntity
    {
        [SerializeField] protected string m_Name;
        [SerializeField] protected int m_Depth;
        [SerializeField] protected int m_ID;

        [System.NonSerialized] private TreeElementEntity m_Parent;
        [System.NonSerialized] private List<TreeElementEntity> m_Childs = new List<TreeElementEntity>();

        public bool enabled;

        public virtual string Name => m_Name;
        public int Depth { get => m_Depth; set => m_Depth = value; }
        public int ID { get => m_ID; }

        public bool HasChilds => m_Childs != null && m_Childs.Count > 0;

        public TreeElementEntity Parent { get => m_Parent; set => m_Parent = value; }
        public List<TreeElementEntity> Childs { get => m_Childs; set => m_Childs = value; }

        public TreeElementEntity(string name, int depth, int id)
        {
            m_Name = name;
            m_Depth = depth;
            m_ID = id;
        }

        public virtual void DrawGUI(Rect rect, int column)
        {
        }
    }
}