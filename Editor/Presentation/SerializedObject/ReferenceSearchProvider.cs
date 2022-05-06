using Syadeu.Collections;
using Syadeu.Presentation;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    internal sealed class ReferenceSearchProvider : SearchProviderBase
    {
        private Type m_TargetType;
        private Action<Hash> m_OnClick;
        private Predicate<ObjectBase> m_Predicate;

        public ReferenceSearchProvider(Type targetType, Action<Hash> onClick, Predicate<ObjectBase> predicate)
        {
            m_TargetType = targetType;
            m_OnClick = onClick;
            m_Predicate = predicate;
        }

        public override List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> list = new List<SearchTreeEntry>();
            List<Type> types = new List<Type>();

            if (m_TargetType == null)
            {
                foreach (ObjectBase obj in EntityDataList.Instance.m_Objects.Values)
                {
                    if (m_Predicate != null && !m_Predicate.Invoke(obj)) continue;

                    Type entityType = obj.GetType();

                    int index = ConstructGroups(types, list, entityType);

                    SearchTreeEntry entry = new SearchTreeEntry(
                        new UnityEngine.GUIContent(obj.Name, CoreGUI.EmptyIcon));
                    entry.userData = obj.Hash;
                    entry.level = list[index].level + 1;

                    list.Insert(index + 1, entry);
                    types.Insert(index + 1, entityType);
                }
            }
            else
            {
                foreach (ObjectBase obj in EntityDataList.Instance.m_Objects.Values)
                {
                    if (m_Predicate != null && !m_Predicate.Invoke(obj)) continue;

                    Type entityType = obj.GetType();

                    if (!TypeHelper.InheritsFrom(entityType, m_TargetType)) continue;

                    int index = ConstructGroups(types, list, entityType);

                    SearchTreeEntry entry = new SearchTreeEntry(
                        new UnityEngine.GUIContent(obj.Name, CoreGUI.EmptyIcon));
                    entry.userData = obj.Hash;
                    entry.level = list[index].level + 1;

                    list.Insert(index + 1, entry);
                    types.Insert(index + 1, entityType);
                }
            }

            list.Insert(0, new SearchTreeGroupEntry(new GUIContent("References")));
            list.Insert(1, new SearchTreeEntry(new GUIContent("None", CoreGUI.EmptyIcon))
            {
                userData = Hash.Empty,
                level = 1,
            });

            return list;
        }
        public override bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (SearchTreeEntry is SearchTreeGroupEntry) return true;

            m_OnClick?.Invoke((Hash)SearchTreeEntry.userData);
            return true;
        }

        private static int ConstructGroups(List<Type> list, List<SearchTreeEntry> entries, Type type)
        {
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            int index = list.IndexOf(type);
            if (index < 0)
            {
                SearchTreeGroupEntry groupEntry = new SearchTreeGroupEntry(
                    new UnityEngine.GUIContent(TypeHelper.ToString(type)),
                    TypeHelper.GetDepthFrom(type, TypeHelper.TypeOf<ObjectBase>.Type)
                    );

                int parentIndex = list.IndexOf(type.BaseType);
                if (parentIndex < 0)
                {
                    if (!type.BaseType.Equals(TypeHelper.TypeOf<ObjectBase>.Type))
                    {
                        parentIndex = ConstructGroups(list, entries, type.BaseType);
                        if (parentIndex < 0) throw new Exception($"?? {type.FullName} :: {type.BaseType?.FullName}");
                    }
                    else
                    {
                        parentIndex = list.Count - 1;
                    }
                }

                index = parentIndex + 1;

                list.Insert(parentIndex + 1, type);
                entries.Insert(parentIndex + 1, groupEntry);
            }

            return index;
        }
    }
}
