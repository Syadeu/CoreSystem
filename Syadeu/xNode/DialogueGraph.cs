using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if XNODE
using XNode;

namespace Syadeu.xNode
{
    [Serializable, CreateAssetMenu(fileName = "newDialogue", menuName = "Syadeu/xNode/Dialogue Graph")]
    public class DialogueGraph : NodeGraph
    {
        public int Current { get; private set; } = 0;

        public void Initialize()
        {
            Current = 0;

        }
        public void MoveNext()
        {
            if (nodes[Current] is DialogueNode dialogue)
            {

            }
            else if (nodes[Current] is DialogueBranchNode branch)
            {

            }

            Current++;
        }
    }

    [Serializable]
    public class DialogueData
    {
        public DialogueNodeBase m_StartNode;
        public string m_Text;
    }

    public abstract class DialogueNodeBase : Node
    {
        [Input] public DialogueData m_Previous;
    }

    public sealed class DialogueNode : DialogueNodeBase
    {
        public string m_Text;
        [Output(dynamicPortList = true)] public DialogueData[] m_Data;
    }
    public sealed class DialogueBranchNode : DialogueNodeBase
    {
        [Output] public DialogueData m_Data;
    }
}
#endif
