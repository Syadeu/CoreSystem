using System.Collections.Generic;
using System.Linq;

namespace SyadeuEditor.Tree
{
    public class TreeEntity
    {
        private List<TreeElementEntity> m_Data;
        private int m_CurrentID = 0;

        public TreeFolderElement Root => m_Data[0] as TreeFolderElement;

        internal TreeEntity() { }
        //public TreeEntity(IList<TreeElementEntity> list)
        //{
        //    m_Data = new List<TreeElementEntity>(list);
        //}

        public int GetUniqueID()
        {
            int val = m_CurrentID;
            m_CurrentID++;
            return val;
        }

        #region Tools

        public void AddElements<T>(IList<T> elements, TreeElementEntity parent, int insertPosition) where T : TreeElementEntity
        {
            if (elements == null)
                throw new System.ArgumentNullException(nameof(elements), "elements is null");
            if (elements.Count == 0)
                throw new System.ArgumentNullException(nameof(elements), "elements Count is 0: nothing to add");
            if (parent == null)
                throw new System.ArgumentNullException(nameof(parent), "parent is null");

            if (parent.Childs == null)
                parent.Childs = new List<TreeElementEntity>();

            parent.Childs.InsertRange(insertPosition, elements.Cast<TreeElementEntity>());
            foreach (var element in elements)
            {
                element.Parent = parent;
                element.Depth = parent.Depth + 1;
                UpdateDepthValues(element);
            }

            TreeToList(Root, m_Data);

            //Changed();
        }
        public TreeEntity AddAtLast(TreeElementEntity element)
        {
            m_Data.Add(element);
            return this;
        }
        public TreeEntity AddAfter(int id, TreeElementEntity element)
        {
            m_Data.Insert(GetDataIdx(id) + 1, element);

            return this;
        }
        public TreeEntity AddInside(int id, TreeElementEntity element)
        {
            int parentIdx = GetDataIdx(id);
            TreeElementEntity parent = m_Data[parentIdx];

            int lastChildIdx = GetLastChildIdx(parentIdx, parent);

            element.Depth = parent.Depth + 1;
            if (element.Parent != null)
            {
                element.Parent.Childs.Remove(element);
                element.Parent = parent;
            }
            parent.Childs.Add(element);

            m_Data.Insert(lastChildIdx, element);

            if (element.HasChilds)
            {
                for (int i = 0; i < element.Childs.Count; i++)
                {
                    AddInside(element.ID, element.Childs[i]);
                }
            }

            return this;
        }
        public void MoveElements(TreeElementEntity parentElement, int insertionIndex, List<TreeElementEntity> elements)
        {
            if (insertionIndex < 0)
                throw new System.ArgumentException("Invalid input: insertionIndex is -1, client needs to decide what index elements should be reparented at");

            // Invalid reparenting input
            if (parentElement == null)
                return;

            // We are moving items so we adjust the insertion index to accomodate that any items above the insertion index is removed before inserting
            if (insertionIndex > 0)
                insertionIndex -= parentElement.Childs.GetRange(0, insertionIndex).Count(elements.Contains);

            // Remove draggedItems from their parents
            foreach (var draggedItem in elements)
            {
                draggedItem.Parent.Childs.Remove(draggedItem);    // remove from old parent
                draggedItem.Parent = parentElement;                 // set new parent
            }

            if (parentElement.Childs == null)
                parentElement.Childs = new List<TreeElementEntity>();

            // Insert dragged items under new parent
            parentElement.Childs.InsertRange(insertionIndex, elements);

            UpdateDepthValues(Root);
            TreeToList(Root, m_Data);

            //Changed();
        }

        #endregion

        #region Gets
        public TreeElementEntity Find(int id) => m_Data.FirstOrDefault(element => element.ID == id);
        public int GetDataIdx(int id)
        {
            int idx = -1;
            for (int i = 0; i < m_Data.Count; i++)
            {
                if (m_Data[i].ID.Equals(id))
                {
                    idx = i;
                    break;
                }
            }
            if (idx < 0) throw new System.Exception();
            return idx;
        }
        public int GetLastChildIdx(int parentIdx, TreeElementEntity parent)
        {
            int lastChildIdx = parentIdx + 1;
            for (int i = lastChildIdx; i < m_Data.Count; i++, lastChildIdx++)
            {
                if (m_Data[i].Depth >= parent.Depth) break;
            }
            return lastChildIdx;
        }
        public IList<int> GetAncestors(int id)
        {
            var parents = new List<int>();
            TreeElementEntity T = Find(id);
            if (T != null)
            {
                while (T.Parent != null)
                {
                    parents.Add(T.Parent.ID);
                    T = T.Parent;
                }
            }
            return parents;
        }
        public IList<int> GetDescendantsThatHaveChildren(int id)
        {
            TreeElementEntity searchFromThis = Find(id);
            if (searchFromThis != null)
            {
                return GetParentsBelowStackBased(searchFromThis);
            }
            return new List<int>();
        }
        private static IList<int> GetParentsBelowStackBased(TreeElementEntity searchFromThis)
        {
            Stack<TreeElementEntity> stack = new Stack<TreeElementEntity>();
            stack.Push(searchFromThis);

            var parentsBelow = new List<int>();
            while (stack.Count > 0)
            {
                TreeElementEntity current = stack.Pop();
                if (current.HasChilds)
                {
                    parentsBelow.Add(current.ID);
                    foreach (var T in current.Childs)
                    {
                        stack.Push(T);
                    }
                }
            }

            return parentsBelow;
        }

        #endregion

        #region Statics
        public static TreeEntity GetTree()
        {
            TreeEntity tree = new TreeEntity();
            tree.m_Data = new List<TreeElementEntity>()
            {
                new TreeFolderElement("master", -1, tree.GetUniqueID())
            };

            return tree;
        }
        public static void TreeToList<T>(T root, IList<T> result) where T : TreeElementEntity
        {
            if (result == null)
                throw new System.NullReferenceException("The input 'IList<T> result' list is null");
            result.Clear();

            Stack<T> stack = new Stack<T>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                T current = stack.Pop();
                result.Add(current);

                if (current.Childs != null && current.Childs.Count > 0)
                {
                    for (int i = current.Childs.Count - 1; i >= 0; i--)
                    {
                        stack.Push((T)current.Childs[i]);
                    }
                }
            }
        }
        // For updating depth values below any given element e.g after reparenting elements
        public static void UpdateDepthValues<T>(T root) where T : TreeElementEntity
        {
            if (root == null)
                throw new System.ArgumentNullException(nameof(root), "The root is null");

            if (!root.HasChilds)
                return;

            Stack<TreeElementEntity> stack = new Stack<TreeElementEntity>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                TreeElementEntity current = stack.Pop();
                if (current.Childs != null)
                {
                    foreach (var child in current.Childs)
                    {
                        child.Depth = current.Depth + 1;
                        stack.Push(child);
                    }
                }
            }
        }
        #endregion
    }
}