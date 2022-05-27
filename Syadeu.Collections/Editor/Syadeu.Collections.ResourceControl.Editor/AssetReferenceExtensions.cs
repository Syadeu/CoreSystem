// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if UNITY_2019_1_OR_NEWER && UNITY_ADDRESSABLES
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Syadeu.Collections.ResourceControl.Editor
{
    public static class AssetReferenceExtensions
    {
        public static string GetAssetPath(this UnityEngine.AddressableAssets.AssetReference t) 
        {
            return AssetDatabase.GetAssetPath(t.editorAsset);
        }
        public static AddressableAssetEntry GetAssetEntry(this UnityEngine.AddressableAssets.AssetReference t) 
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var entry = settings.FindAssetEntry(t.AssetGUID);
            
            return entry;
        }
        public static AddressableAssetGroup GetAssetGroup(this UnityEngine.AddressableAssets.AssetReference t) 
        {
            var entry = t.GetAssetEntry();
            if (entry == null) return null;

            AddressableAssetGroup group = entry.parentGroup;
            return group;
        }
    }
}

#endif