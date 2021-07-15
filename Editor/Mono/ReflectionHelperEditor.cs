using System;
using System.Reflection;
using Syadeu.Database;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using UnityEditor;

namespace SyadeuEditor
{
    public sealed class ReflectionHelperEditor
    {
        public static void DrawMember(object ins, MemberInfo memberInfo, int depth = 0)
        {
            Type declaredType;
            Action<object, object> setter;
            Func<object, object> getter;
            if (memberInfo is FieldInfo field)
            {
                declaredType = field.FieldType;
                setter = field.SetValue;
                getter = field.GetValue;
            }
            else if (memberInfo is PropertyInfo property)
            {
                declaredType = property.PropertyType;
                setter = property.SetValue;
                getter = property.GetValue;
            }
            else throw new NotImplementedException();
            string name = ReflectionHelper.SerializeMemberInfoName(memberInfo);

            if (declaredType.Equals(TypeHelper.TypeOf<int>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.IntField(name, (int)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<float>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.FloatField(name, (float)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<double>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.DoubleField(name, (double)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<long>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.LongField(name, (long)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<ulong>.Type))
            {
                setter.Invoke(ins, ulong.Parse(EditorGUILayout.LongField(name, (long.Parse(getter.Invoke(ins).ToString()))).ToString()));
            }
            #region Unity Types
            else if (declaredType.Equals(TypeHelper.TypeOf<UnityEngine.Rect>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.RectField(name, (UnityEngine.Rect)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<UnityEngine.RectInt>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.RectIntField(name, (UnityEngine.RectInt)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<UnityEngine.Vector3>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.Vector3Field(name, (UnityEngine.Vector3)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<UnityEngine.Vector3Int>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.Vector3IntField(name, (UnityEngine.Vector3Int)getter.Invoke(ins)));
            }
            #endregion
            else if (declaredType.Equals(TypeHelper.TypeOf<string>.Type))
            {
                setter.Invoke(ins, EditorGUILayout.TextField(name, (string)getter.Invoke(ins)));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<Hash>.Type))
            {
                setter.Invoke(ins, new Hash(ulong.Parse(EditorGUILayout.LongField(name, (long.Parse(getter.Invoke(ins).ToString()))).ToString())));
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<ValuePairContainer>.Type))
            {
                ((ValuePairContainer)getter.Invoke(ins)).DrawValueContainer(name);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<LuaScript>.Type))
            {
                LuaScript scr = (LuaScript)getter.Invoke(ins);
                if (scr == null)
                {
                    scr = string.Empty;
                    setter.Invoke(ins, scr);
                }
                scr.DrawFunctionSelector(name);
            }
            else if (declaredType.Equals(TypeHelper.TypeOf<LuaScriptContainer>.Type))
            {
                ((LuaScriptContainer)getter.Invoke(ins)).DrawGUI(name);
            }
            else
            {
                if (depth > 4) return;

                EditorGUILayout.LabelField(name);
                EditorGUI.indentLevel += 1;
                MemberInfo[] insider = ReflectionHelper.GetSerializeMemberInfos(declaredType);
                for (int i = 0; i < insider.Length; i++)
                {
                    //EditorGUILayout.LabelField(insider[i].Name);
                    object temp = getter.Invoke(ins);
                    DrawMember(temp, insider[i], depth++);
                }

                EditorGUI.indentLevel -= 1;
            }
        }
    }
}
