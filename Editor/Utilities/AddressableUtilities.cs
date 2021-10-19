using Syadeu.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SyadeuEditor.Utilities
{
    public sealed class AddressableUtilities : CLRSingleTone<AddressableUtilities>
    {
        public static void DrawAssetReference(string name, Action<AssetReference> setter, AssetReference refAsset)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(refAsset?.AssetGUID);

            string displayName;
            AddressableAssetEntry entry = null;
            if (refAsset != null /*&& refAsset.IsValid()*/)
            {
                entry = AddressableAssetSettingsDefaultObject.GetSettings(true).FindAssetEntry(refAsset.AssetGUID);
                if (entry == null)
                {
                    displayName = "Not Addressable: " + assetPath.Split('/').Last();
                }
                else displayName = entry.address.Split('/').Last();
            }
            else displayName = "None";

            EditorGUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(name)) EditorGUILayout.LabelField(name);

            if (GUILayout.Button(displayName, EditorStyleUtilities.SelectorStyle, GUILayout.ExpandWidth(true)))
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;

                PopupWindow.Show(rect, AssetReferencePopup.GetWindow(setter, refAsset?.AssetGUID, displayName));
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
