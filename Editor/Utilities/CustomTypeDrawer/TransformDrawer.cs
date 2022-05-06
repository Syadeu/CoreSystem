using Syadeu.Collections.Proxy;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public sealed class TransformDrawer : ObjectDrawer<ITransform>
    {
        public TransformDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public override ITransform Draw(ITransform currentValue)
        {
            using (new CoreGUI.BoxBlock(Color.black))
            {
                EditorUtilities.StringRich("Transform", 15);
                EditorGUI.indentLevel++;

                currentValue.position =
                EditorGUILayout.Vector3Field("Position", currentValue.position);

                Vector3 eulerAngles = currentValue.eulerAngles;

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    eulerAngles = EditorGUILayout.Vector3Field("Rotation", eulerAngles);

                    if (change.changed)
                    {
                        currentValue.eulerAngles = eulerAngles;
                    }
                }

                currentValue.scale
                    = EditorGUILayout.Vector3Field("Scale", currentValue.scale);

                EditorGUI.indentLevel--;
            }

            return currentValue;
        }
    }
}
