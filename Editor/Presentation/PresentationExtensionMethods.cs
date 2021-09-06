using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System.Reflection;
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
        public static UnityEngine.Object GetEditorAsset(this IPrefabReference prefab)
        {
            if (m_RefPrefabField == null)
            {
                m_RefPrefabField = TypeHelper.TypeOf<PrefabList.ObjectSetting>.Type
                    .GetField("m_RefPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (prefab == null) return null;

            object value = m_RefPrefabField.GetValue(prefab.GetObjectSetting());
            if (value == null) return null;

            AssetReference asset = (AssetReference)value;
            return asset.editorAsset;
        }
    }
}
