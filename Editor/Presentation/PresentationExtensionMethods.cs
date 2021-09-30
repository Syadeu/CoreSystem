using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SyadeuEditor.Presentation
{
    public static class PresentationExtensionMethods
    {
        private static FieldInfo m_RefPrefabField = null;

        public static UnityEngine.Object GetEditorAsset(this PrefabList.ObjectSetting objectSetting)
        {
            if (m_RefPrefabField == null)
            {
                m_RefPrefabField = TypeHelper.TypeOf<PrefabList.ObjectSetting>.Type
                    .GetField("m_RefPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (objectSetting == null) return null;

            object value = m_RefPrefabField.GetValue(objectSetting);
            if (value == null) return null;

            AssetReference asset = (AssetReference)value;
            return asset.editorAsset;
        }
        public static string GetEditorAssetPath(this IPrefabReference prefab)
        {
            if (m_RefPrefabField == null)
            {
                m_RefPrefabField = TypeHelper.TypeOf<PrefabList.ObjectSetting>.Type
                    .GetField("m_RefPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (prefab == null) return string.Empty;

            PrefabList.ObjectSetting set = prefab.GetObjectSetting();
            if (set == null) return string.Empty;

            object value = m_RefPrefabField.GetValue(set);
            if (value == null) return string.Empty;

            AssetReference asset = (AssetReference)value;
            return AssetDatabase.GetAssetPath(asset.editorAsset);
        }
        public static UnityEngine.Object GetEditorAsset(this IPrefabReference prefab)
        {
            if (m_RefPrefabField == null)
            {
                m_RefPrefabField = TypeHelper.TypeOf<PrefabList.ObjectSetting>.Type
                    .GetField("m_RefPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (prefab == null) return null;

            PrefabList.ObjectSetting set = prefab.GetObjectSetting();
            if (set == null) return null;

            object value = m_RefPrefabField.GetValue(set);
            if (value == null) return null;

            AssetReference asset = (AssetReference)value;
            return asset.editorAsset;
        }

        const string c_JsonFilePath = "{0}/{1}.json";
        public static string GetRawJson(this ObjectBase obj)
        {
            Type objType = obj.GetType();
            string objPath;
            if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.EntityPath, objType.Name);
            }
            else if (TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.AttributePath, objType.Name);
            }
            else if (TypeHelper.TypeOf<ActionBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.ActionPath, objType.Name);
            }
            else if (TypeHelper.TypeOf<DataObjectBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.DataPath, objType.Name);
            }
            else
            {
                throw new NotImplementedException();
            }

            string filePath = string.Format(c_JsonFilePath, objPath, EntityDataList.ToFileName(obj));
            if (!Directory.Exists(objPath) /*|| !File.Exists(filePath)*/) return string.Empty;
            return File.ReadAllText(filePath);
        }
    }
}
