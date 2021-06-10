using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Syadeu;

#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
using Syadeu.Mono.Audio;
#endif

namespace SyadeuEditor
{
    public sealed class ItemDesigner : EditorWindow
    {

    }
}

namespace SyadeuEditor.Database
{
    [Serializable]
    public class TreeElement
    {
        [SerializeField] private int m_Idx;
        [SerializeField] private string m_Name;
        [SerializeField] private int m_Depth;

        [NonSerialized] private TreeElement m_Parent;
        [NonSerialized] private List<TreeElement> m_Children;

        public int Depth { get => m_Depth; set => m_Depth = value; }
		public TreeElement Parent
		{
			get { return m_Parent; }
			set { m_Parent = value; }
		}

		public List<TreeElement> Children
		{
			get { return m_Children; }
			set { m_Children = value; }
		}

		public bool HasChildren => Children != null && Children.Count > 0;

		public string Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public int Idx
		{
			get { return m_Idx; }
			set { m_Idx = value; }
		}

		public TreeElement(string name, int depth, int idx)
		{
			m_Name = name;
			m_Idx = idx;
			m_Depth = depth;
		}
	}

	public abstract class TreeEntity
    {
		public static bool VaildateTreeElements<T>(IList<T> data) where T : TreeElement
		{
			if (data.Count == 0)
			{
				$"data count is 0".ToLogError();
				return false;
			}
			if (data[0].Depth >= 0) throw new Exception();

			return true;
		}
		public static T ListToTree<T>(IList<T> data) where T : TreeElement
		{
			if (data == null) throw new ArgumentNullException("data", "Input data is null. Ensure input is a non-null list.");
			if (!VaildateTreeElements(data)) return null;

			for (int i = 0; i < data.Count; i++)
			{
				T parent = data[i];
				List<TreeElement> childs = null;

				for (int j = i + 1; j < data.Count; j++)
				{
					if (data[j].Depth <= parent.Depth) break;

					if (data[j].Depth == parent.Depth + 1)
					{
						data[j].Parent = parent;
						if (childs == null) childs = new List<TreeElement>();
						childs.Add(data[j]);
					}
				}

				parent.Children = childs;
			}

			return data[0];
		}

		static bool IsChildOf<T>(T child, IList<T> elements) where T : TreeElement
		{
			while (child != null)
			{
				child = (T)child.Parent;
				if (elements.Contains(child)) return true;
			}
			return false;
		}

		public static IList<T> FindCommonAncestorsWithinList<T>(IList<T> elements) where T : TreeElement
		{
			if (elements.Count == 1) return new List<T>(elements);

			List<T> result = new List<T>(elements);
			result.RemoveAll(g => IsChildOf(g, elements));
			return result;
		}
	}

	public abstract class TreeModel<T> : TreeEntity, IInitialize<IList<T>> where T : TreeElement
    {
		private IList<T> m_Data;
		private T m_Root;
		private int m_MaxIdx;

		public T Root => m_Root;
		public int Count => m_Data.Count;

		public TreeModel() { }
		public TreeModel(IList<T> data)
        {
			Initialize(data);
        }

		public void Initialize(IList<T> data)
        {
            m_Data = data ?? throw new ArgumentNullException("data", "Input data is null. Ensure input is a non-null list.");

			m_MaxIdx = m_Data.Max(e => e.Idx);

			if (Count > 0)
			{
				m_Root = ListToTree(data);
			}
			else m_Root = m_Data[0];
		}

		public void RemoveElements(params T[] elements)
        {
			IList<T> targets = FindCommonAncestorsWithinList(elements);
            for (int i = 0; i < targets.Count; i++)
            {
				targets[i].Parent.Children.Remove(targets[i]);
				targets[i].Parent = null;
			}


        }
    }
}
