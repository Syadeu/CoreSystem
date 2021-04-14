using Syadeu.Mono.XNode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Syadeu.Mono.Creature
{
	public abstract class CreatureNodeEntity : CoreSystemNodeEntity { }
	[DisallowMultipleNodes]
	public sealed class CreatureEntryPointNode : CreatureNodeEntity
	{
		public CreatureBrain m_CreatureBrain;
		[Output] public Node m_Output;

		//[Output(dynamicPortList = true)] public CreatureNodeEntity[] m_Outputs;

#if UNITY_EDITOR
		private void Reset()
        {
			name = "Entry Point";
        }
#endif
		
	}
}

namespace Syadeu.Mono.XNode
{
	public abstract class CoreSystemNodeEntity : Node
    {
		public virtual void Trigger() { }
	}
	public abstract class LogicNodeEntity : CoreSystemNodeEntity { }

	public abstract class IfLogic<T> : LogicNodeEntity
    {
		[Input] public T m_Value;
		public T m_ExpectedValue;

		[Output] public Node m_IsTrue;
		[Output] public Node m_IsFalse;

		public override object GetValue(NodePort port)
        {
			T[] inputs = port.GetInputValues<T>();
            for (int i = 0; i < inputs.Length; i++)
            {
				if (inputs[i].Equals(m_ExpectedValue)) return m_IsTrue;
            }
			return m_IsFalse;
        }
#if UNITY_EDITOR
        private void Reset()
		{
			name = $"If {typeof(T).Name}";
		}
#endif
	}
	public sealed class IfIntLogic : IfLogic<int> { }
	public sealed class IfFloatLogic : IfLogic<float> { }
	public sealed class IfBoolLogic : IfLogic<bool> { }
	public sealed class IfStringLogic : IfLogic<string> { }

	public abstract class WaitForLogic<T> : LogicNodeEntity
    {
		[Input(connectionType = ConnectionType.Override)] public T m_Value;
		[Input] public CoreSystemNodeEntity m_WhileWait;
		public T m_ExpectedValue;

		[Output] public Node m_Output;

        private struct Enumerable : IEnumerable<bool>
        {
			private readonly T value;
			private readonly T waitValue;
			private readonly CoreSystemNodeEntity[] whileWait;

			public Enumerable(T input, T expected, params CoreSystemNodeEntity[] whileWaitTrigger)
            {
				value = input;
				waitValue = expected;
				whileWait = whileWaitTrigger;
			}

            public IEnumerator<bool> GetEnumerator()
            {
                while (!value.Equals(waitValue))
                {
                    for (int i = 0; i < whileWait.Length; i++)
                    {
						whileWait[i].Trigger();
					}
					
					yield return false;
                }
				yield return true;
            }
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
        public override object GetValue(NodePort port)
        {
			var temp = GetInputPort("m_WhileWait");
			
			return new Enumerable(port.GetInputValue<T>(), m_ExpectedValue, temp.GetInputValues<CoreSystemNodeEntity>()).GetEnumerator();
        }
#if UNITY_EDITOR
        private void Reset()
		{
			name = $"WaitFor {typeof(T).Name}";
		}
#endif
	}
	public sealed class WaitForIntLogic : WaitForLogic<int> { }
	public sealed class WaitForFloatLogic : WaitForLogic<float> { }
	public sealed class WaitForBoolLogic : WaitForLogic<bool> { }
	public sealed class WaitForStringLogic : WaitForLogic<string> { }

	public sealed class WaitForTimeLogic : LogicNodeEntity
    {
		[Input] public Node m_Node;
		public float m_Seconds;
		public bool m_Realtime;

    }
}