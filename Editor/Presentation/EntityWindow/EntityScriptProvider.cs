using Syadeu.Collections;
using Syadeu.Presentation;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor.Presentation
{
    public static class EntityScriptProvider
    {
        private static readonly Dictionary<Type, MonoScript> s_ScriptCache = new Dictionary<Type, MonoScript>();
        private static readonly Dictionary<Type, MonoScript> s_DrawerScriptCache = new Dictionary<Type, MonoScript>();
        private static readonly Dictionary<Type, Type> s_DrawerPerType = new Dictionary<Type, Type>();

        private static FieldInfo s_CachedPropertyTypeField, s_CachedPropertyUseChildField;
        private static FieldInfo CachedPropertyTypeField
        {
            get
            {
                if (s_CachedPropertyTypeField == null)
                {
                    const string c_Name = "m_Type";

                    Type drawerAttType = TypeHelper.TypeOf<CustomPropertyDrawer>.Type;
                    s_CachedPropertyTypeField = drawerAttType.GetField(c_Name, BindingFlags.NonPublic | BindingFlags.Instance);
                }

                return s_CachedPropertyTypeField;
            }
        }
        private static FieldInfo CachedPropertyUseChildField
        {
            get
            {
                if (s_CachedPropertyUseChildField == null)
                {
                    const string c_Name = "m_UseForChildren";

                    Type drawerAttType = TypeHelper.TypeOf<CustomPropertyDrawer>.Type;
                    s_CachedPropertyUseChildField = drawerAttType.GetField(c_Name, BindingFlags.NonPublic | BindingFlags.Instance);
                }

                return s_CachedPropertyUseChildField;
            }
        }

        static EntityScriptProvider()
        {
            BuildScriptCache();
        }
        private static void BuildScriptCache()
        {
            foreach (var entityType in TypeCache.GetTypesDerivedFrom<ObjectBase>())
            {
                if (entityType.IsAbstract) continue;

                AddEntityScriptAsset(entityType);
            }

#pragma warning disable SD0001 // typeof(T) 는 매 호출시마다 Reflection 으로 새로 값을 받아옵니다.
            foreach (var entityDrawerType in TypeCache.GetTypesDerivedFrom(typeof(PropertyDrawer<>)))
#pragma warning restore SD0001 // typeof(T) 는 매 호출시마다 Reflection 으로 새로 값을 받아옵니다.
            {
                if (entityDrawerType.IsAbstract) continue;

                AddEntityDrawerScriptAsset(entityDrawerType);
            }
        }
        private static void AddEntityScriptAsset(Type type)
        {
            MonoScript script = ScriptUtilities.FindScriptFromClassName(type.Name);
            if (script == null) return;

            s_ScriptCache.Add(type, script);
        }
        private static void AddEntityDrawerScriptAsset(Type type)
        {
            const string
                c_DrawerPostFix = "Drawer", c_PropertyDrawerPostFix = "PropertyDrawer";

            IEnumerable<CustomPropertyDrawer> drawers = type.GetCustomAttributes<CustomPropertyDrawer>();
            if (!drawers.Any()) return;

            foreach (var drawer in drawers)
            {
                Type entityType = (Type)CachedPropertyTypeField.GetValue(drawer);
                bool useChild = (bool)CachedPropertyUseChildField.GetValue(drawer);

                MonoScript script = ScriptUtilities.FindScriptFromClassName(type.Name);
                if (script == null)
                    script = ScriptUtilities.FindScriptFromClassName(type.Name + c_DrawerPostFix);
                if (script == null)
                    script = ScriptUtilities.FindScriptFromClassName(type.Name + c_PropertyDrawerPostFix);

                s_DrawerPerType[entityType] = type;
                s_DrawerScriptCache[type] = script;
                
                //if (!m_DrawerPerType.TryGetValue(entityType, out Type existingDrawerType))
                //{
                //    m_DrawerPerType.Add(entityType, type);
                //    m_DrawerScriptCache.Add(type, script);

                //    return;
                //}
            }
        }

        public static bool HasDrawerScript(Type t)
        {
            return s_DrawerPerType.ContainsKey(t);
        }

        public static void OpenScript(Type type)
        {
            if (!s_ScriptCache.TryGetValue(type, out var script)) return;

            AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
        }
        public static void OpenDrawerScript(Type type)
        {
            if (!s_DrawerPerType.TryGetValue(type, out var drawerType)) return;
            
            if (!s_DrawerScriptCache.TryGetValue(drawerType, out var script)) return;

            AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
        }
    }
}
