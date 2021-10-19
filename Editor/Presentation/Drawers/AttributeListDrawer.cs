using Syadeu.Collections;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class AttributeListDrawer : ObjectDrawer<Reference<AttributeBase>[]>
    {
        private List<ObjectDrawerBase> m_Drawers = new List<ObjectDrawerBase>();
        private List<bool> m_Open = new List<bool>();

        public AttributeListDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            Reload();
        }
        private void Reload()
        {
            m_Drawers.Clear();
            m_Open.Clear();

            Reference<AttributeBase>[] arr = Getter.Invoke();

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].IsValid()) m_Drawers.Add(ObjectBaseDrawer.GetDrawer(arr[i]));
                else m_Drawers.Add(null);

                m_Open.Add(false);
            }
        }
        public override Reference<AttributeBase>[] Draw(Reference<AttributeBase>[] currentValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorUtilities.StringRich(Name, 15);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                Reference<AttributeBase>[] copy = new Reference<AttributeBase>[currentValue.Length + 1];
                if (currentValue.Length > 0)
                {
                    Array.Copy(currentValue, copy, currentValue.Length);
                }

                currentValue = copy;

                m_Drawers.Add(null);
                m_Open.Add(false);
            }
            if (currentValue.Length > 0 && GUILayout.Button("-", GUILayout.Width(20)))
            {
                Reference<AttributeBase>[] copy = new Reference<AttributeBase>[currentValue.Length - 1];
                if (currentValue.Length > 0)
                {
                    Array.Copy(currentValue, copy, copy.Length);
                }

                currentValue = copy;
                m_Drawers.RemoveAt(m_Drawers.Count - 1);
                m_Open.RemoveAt(m_Open.Count - 1);
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorUtilities.BoxBlock(Color.black))
            {
                for (int i = 0; i < currentValue.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    int idx = i;
                    EditorGUI.BeginChangeCheck();
                    idx = EditorGUILayout.DelayedIntField(idx, GUILayout.Width(40));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (idx >= currentValue.Length) idx = currentValue.Length - 1;

                        Reference<AttributeBase> cache = currentValue[i];
                        bool cacheOpen = m_Open[i];
                        var cacheDrawer = m_Drawers[i];

                        var temp = currentValue.ToList();
                        temp.RemoveAt(i);
                        m_Open.RemoveAt(i);
                        m_Drawers.RemoveAt(i);

                        temp.Insert(idx, cache);
                        m_Open.Insert(idx, cacheOpen);
                        m_Drawers.Insert(idx, cacheDrawer);

                        currentValue = temp.ToArray();
                        Setter.Invoke(currentValue);
                    }

                    idx = i;
                    ReflectionHelperEditor.DrawAttributeSelector(null, (attHash) =>
                    {
                        currentValue[idx] = new Reference<AttributeBase>(attHash);

                        AttributeBase targetAtt = currentValue[idx].GetObject();
                        if (targetAtt != null)
                        {
                            m_Drawers[idx] = ObjectBaseDrawer.GetDrawer(targetAtt);
                        }
                    }, currentValue[idx], TargetObject.GetType());

                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        if (currentValue.Length == 1)
                        {
                            currentValue = Array.Empty<Reference<AttributeBase>>();
                            m_Open.Clear();
                            m_Drawers.Clear();
                        }
                        else
                        {
                            var temp = currentValue.ToList();
                            temp.RemoveAt(i);
                            m_Open.RemoveAt(i);
                            m_Drawers.RemoveAt(i);
                            currentValue = temp.ToArray();
                            Setter.Invoke(currentValue);
                        }

                        EditorGUILayout.EndHorizontal();
                        i--;
                        continue;
                    }

                    m_Open[i] = GUILayout.Toggle(m_Open[i], m_Open[i] ? EditorStyleUtilities.FoldoutOpendString : EditorStyleUtilities.FoldoutClosedString, EditorStyleUtilities.MiniButton, GUILayout.Width(20));

                    if (GUILayout.Button("C", GUILayout.Width(20)))
                    {
                        AttributeBase cloneAtt = (AttributeBase)EntityDataList.Instance.GetObject(currentValue[i]).Clone();

                        cloneAtt.Hash = Hash.NewHash();
                        cloneAtt.Name += "_Clone";
                        EntityDataList.Instance.m_Objects.Add(cloneAtt.Hash, cloneAtt);

                        currentValue[i] = new Reference<AttributeBase>(cloneAtt.Hash);
                        m_Drawers[i] = ObjectBaseDrawer.GetDrawer(cloneAtt);
                    }

                    EditorGUILayout.EndHorizontal();

                    if (m_Open[i])
                    {
                        Color color3 = Color.red;
                        color3.a = .7f;

                        using (new EditorUtilities.BoxBlock(color3))
                        {
                            if (!currentValue[i].IsValid())
                            {
                                EditorGUILayout.HelpBox(
                                    "This attribute is invalid.",
                                    MessageType.Error);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox(
                                    "This is shared attribute. Anything made changes in this inspector view will affect to original attribute directly not only as this entity.",
                                    MessageType.Info);

                                m_Drawers[i].OnGUI();
                                //SetAttribute(m_CurrentList[i], m_AttributeDrawers[i].OnGUI());
                            }
                        }

                        EditorUtilities.Line();
                    }
                }
            }

            return currentValue;
        }

    }
    //[EditorTool("TestTool", typeof(EntityWindow))]
    //public sealed class TestTool : EditorTool
    //{
    //    public override void OnToolGUI(EditorWindow window)
    //    {
    //        EditorGUILayout.LabelField("test");
    //        base.OnToolGUI(window);
    //    }
    //}
}
