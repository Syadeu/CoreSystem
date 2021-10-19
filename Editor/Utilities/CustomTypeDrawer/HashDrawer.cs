using Syadeu.Collections;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor.Utilities
{
    public sealed class HashDrawer : ObjectDrawer<Hash>
    {
        public HashDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Hash Draw(Hash currentValue)
        {
            long temp = EditorGUILayout.LongField(Name, long.Parse(currentValue.ToString()));
            return new Hash(ulong.Parse(temp.ToString()));
        }
    }
}
