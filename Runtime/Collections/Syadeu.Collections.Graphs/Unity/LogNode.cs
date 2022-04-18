using GraphProcessor;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [Serializable, NodeMenuItem("Unity/Log")]
	public sealed class LogNode : BaseNode
    {
		[Input("In", true)]
		public IEnumerable<string> input;
		[SerializeField]
		public string text = string.Empty;

		[Setting("Log Type")]
		public LogType logType = LogType.Log;

		public override string name => "Log";

        protected override void Process()
        {
            foreach (var item in input)
            {
				Debug.Log(item);
            }
        }

		private static void Log(IEnumerable<string> other)
        {
			foreach (var item in other)
			{
				Debug.Log(item);
			}
		}
		private static void LogWarning(IEnumerable<string> other)
        {
			foreach (var item in other)
			{
				Debug.LogWarning(item);
			}
		}
		private static void LogError(IEnumerable<string> other)
        {
			foreach (var item in other)
			{
				Debug.LogError(item);
			}
		}
    }
}
