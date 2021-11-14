using Syadeu.Collections;
using System;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor.Utilities
{
    public sealed class NullDrawer : ObjectDrawerBase
    {
        private string m_Name;

        public override object TargetObject => null;
        public override string Name => m_Name;

        private MemberInfo MemberInfo;

        public NullDrawer(MemberInfo member, Type declaredType)
        {
            MemberInfo = member;
            m_Name = $"Not supported Type: {member.MemberType} {TypeHelper.ToString(declaredType)}";
        }

        public override void OnGUI()
        {
            EditorGUILayout.LabelField(MemberInfo.Name, Name);
        }
    }
}
