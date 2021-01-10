using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if XNODE
using XNode;

namespace Syadeu.XNode
{
    [Serializable, CreateAssetMenu(fileName = "newDialogue", menuName = "Syadeu/xNode/Dialogue Graph")]
    public class DialogueGraph : NodeGraph
    {
        
    }

    public abstract class DialogueNodeBase : Node
    {
        [Input] public DialogueNodeBase m_Previous;
    }

    public sealed class DialogueSelectorNode : DialogueNodeBase
    {
        public string m_Text;
        [Output(dynamicPortList = true)] public DialogueTextNode[] m_Options;
    }
    public sealed class DialogueTextNode : DialogueNodeBase
    {
        [Output] public string m_Text;
    }
}
#endif
