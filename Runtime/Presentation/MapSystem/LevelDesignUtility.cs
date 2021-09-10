using System;

using UnityEngine;
using Syadeu.Internal;
using Syadeu.Database;
using Syadeu.Mono;
using System.Reflection;
using UnityEngine.AddressableAssets;
using Syadeu.Presentation.Entities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Presentation.Map
{
    public static class LevelDesignUtility
    {
#if UNITY_EDITOR
        public const string c_EditorOnly = "EditorOnly";
        private static FieldInfo m_RefPrefabField = null;

        private static UnityEngine.Object GetEditorAsset(this IPrefabReference prefab)
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
#endif

        public static void SelectEntity( Ray ray)
        {

        }

        public static GameObject InstantiateObject(Transform parent, MapDataEntityBase.Object target)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                CoreSystem.Logger.ThreadBlock(nameof(InstantiateObject), ThreadInfo.Unity);
            }
#endif
            GameObject obj;
            if (!target.m_Object.IsValid() ||
                target.m_Object.GetObject().Prefab.IsNone() ||
                !target.m_Object.GetObject().Prefab.IsValid())
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.SetParent(parent);

                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"{target.m_Object.GetObject().Name} is not valid. Returned as Empty Cube");
            }
            else
            {
                EntityBase targetObj = target.m_Object.GetObject();
                UnityEngine.Object temp;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    temp = targetObj.Prefab.GetEditorAsset();
                }
                else
#endif
                {
                    if (targetObj.Prefab.Asset == null)
                    {
                        CoreSystem.Logger.LogError(Channel.Presentation,
                            $"You need to load ({targetObj.Name}) prefab first.");
                        return null;
                    }

                    temp = targetObj.Prefab.Asset;
                }

                if (!(temp is GameObject gameObj))
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.SetParent(parent);

                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Type error, {target.m_Object.GetObject().Name} is not a GameObject. Returned as Empty Cube");
                }
                else
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        obj = (GameObject)PrefabUtility.InstantiatePrefab(gameObj, parent);
                    }
                    else
#endif
                    {
                        obj = UnityEngine.Object.Instantiate(targetObj.Prefab.Asset);
                    }
                }
                //
            }

#if UNITY_EDITOR
            obj.tag = c_EditorOnly;
            obj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
#endif

            Transform tr = obj.transform;

            tr.position = target.m_Translation;
            tr.rotation = target.m_Rotation;
            tr.localScale = target.m_Scale;

            return obj;
        }
    }
}
