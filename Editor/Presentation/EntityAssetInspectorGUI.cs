using UnityEditor;
using Syadeu.Collections;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.AddressableAssets.Build;
using Syadeu;
using System.Linq;

namespace SyadeuEditor.Presentation
{
    using Object = UnityEngine.Object;

    internal class EntityAssetInspectorGUI : IStaticInitializer
    {
        static string s_DefaultPrefabGroupName = "PrefabList";
        static AddressableAssetGroup[] s_EntityGroups = null;
        static string[] s_EntityGroupNames = null;

        static EntityAssetInspectorGUI()
        {
            Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        #region Addressable Utils

        internal static bool GetPathAndGUIDFromTarget(Object target, out string path, out string guid, out Type mainAssetType)
        {
            mainAssetType = null;
            guid = string.Empty;
            path = string.Empty;
            if (target == null)
                return false;
            path = AssetDatabase.GetAssetOrScenePath(target);
            if (!IsPathValidForEntry(path))
                return false;
            guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return false;
            mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (mainAssetType == null)
                return false;
            if (mainAssetType != target.GetType() && !typeof(AssetImporter).IsAssignableFrom(target.GetType()))
                return false;
            return true;
        }
        static HashSet<string> excludedExtensions = new HashSet<string>(new string[] { ".cs", ".js", ".boo", ".exe", ".dll", ".meta" });
        internal static bool IsPathValidForEntry(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (!path.StartsWith("assets", StringComparison.OrdinalIgnoreCase) && !IsPathValidPackageAsset(path))
                return false;
            if (path == CommonStrings.UnityEditorResourcePath ||
                path == CommonStrings.UnityDefaultResourcePath ||
                path == CommonStrings.UnityBuiltInExtraPath)
                return false;
            return !excludedExtensions.Contains(Path.GetExtension(path));
        }
        internal static bool IsPathValidPackageAsset(string path)
        {
            string convertPath = path.ToLower().Replace("\\", "/");
            string[] splitPath = convertPath.Split('/');

            if (splitPath.Length < 3)
                return false;
            if (splitPath[0] != "packages")
                return false;
            if (splitPath.Length == 3)
            {
                string ext = Path.GetExtension(splitPath[2]);
                if (ext == ".json" || ext == ".asmdef")
                    return false;
            }
            return true;
        }

        #endregion

        private static void Validate(out AddressableAssetSettings aaSettings)
        {
            aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (aaSettings == null) return;

            if (s_EntityGroups != null)
            {
                if (aaSettings.groups.Where((other) => other.HasSchema<PrefabListGroupSchema>() || other.Name.Equals(s_DefaultPrefabGroupName)).Count() != s_EntityGroups.Length)
                {
                    s_EntityGroups = null;
                }
            }

            if (s_EntityGroups != null) return;
            
            s_EntityGroups = aaSettings.groups.Where((other) => other.HasSchema<PrefabListGroupSchema>() || other.Name.Equals(s_DefaultPrefabGroupName)).ToArray();

            List<string> names = new List<string>() { "None" };
            names.AddRange(s_EntityGroups.Select((other) => other.Name));
            s_EntityGroupNames = names.ToArray();
        }
        private static int GetGroupIndex(AddressableAssetGroup group)
        {
            if (!s_EntityGroups.Contains(group)) return 0;

            for (int i = 0; i < s_EntityGroups.Length; i++)
            {
                if (s_EntityGroups[i].Equals(group))
                {
                    return i + 1;
                }
            }
            return 0;
        }
        private static AddressableAssetGroup GetIndexToGroup(int idx)
        {
            if (idx < 0 || idx - 1 >= s_EntityGroups.Length) return null;
            return s_EntityGroups[idx - 1];
        }

        private static void OnPostHeaderGUI(Editor editor)
        {
            if (editor.targets.Length == 0) return;

            Validate(out var aaSettings);
            int currentGroupIdx = -1;
            AddressableAssetEntry entry = null;

            int addressableCount = 0;
            bool foundValidAsset = false;
            bool foundAssetGroup = false;

            foundAssetGroup |= editor.target is AddressableAssetGroup;
            foundAssetGroup |= editor.target is AddressableAssetGroupSchema;
            if (GetPathAndGUIDFromTarget(editor.target, out var path, out var guid, out var mainAssetType))
            {
                // Is asset
                if (!BuildUtility.IsEditorAssembly(mainAssetType.Assembly))
                {
                    foundValidAsset = true;

                    if (aaSettings != null)
                    {
                        entry = aaSettings.FindAssetEntry(guid);
                        if (entry != null && !entry.IsSubAsset)
                        {
                            currentGroupIdx = GetGroupIndex(entry.parentGroup);
                            addressableCount++;
                        }
                    }
                }
            }
            if (currentGroupIdx < 0) return;

            if (foundAssetGroup)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("test 123");
                //GUILayout.Label("Profile: " + AddressableAssetSettingsDefaultObject.GetSettings(true).profileSettings.
                //    GetProfileName(AddressableAssetSettingsDefaultObject.GetSettings(true).activeProfileId));

                //GUILayout.FlexibleSpace();
                //if (GUILayout.Button("System Settings", "MiniButton"))
                //{
                //    EditorGUIUtility.PingObject(AddressableAssetSettingsDefaultObject.Settings);
                //    Selection.activeObject = AddressableAssetSettingsDefaultObject.Settings;
                //}
                GUILayout.EndHorizontal();
            }

            if (!foundValidAsset) return;

            if (addressableCount == 0)
            {
                EditorGUILayout.LabelField("not addressable");
                //if (GUILayout.Toggle(false, s_AddressableAssetToggleText, GUILayout.ExpandWidth(false)))
                //    SetAaEntry(AddressableAssetSettingsDefaultObject.GetSettings(true), editor.targets, true);
            }
            else if (addressableCount == editor.targets.Length)
            {
                EditorUtilities.Line();
                string headerString = EditorUtilities.String("Entity", 13);
                if (currentGroupIdx == 0)
                {
                    headerString += EditorUtilities.String(": Invalid", 10);
                }
                else
                {
                    headerString += EditorUtilities.String(": Valid", 10);
                }

                EditorGUILayout.BeginHorizontal();
                EditorUtilities.StringRich(headerString);

                EditorGUI.BeginChangeCheck();
                currentGroupIdx = EditorGUILayout.Popup(currentGroupIdx, s_EntityGroupNames);
                if (EditorGUI.EndChangeCheck())
                {
                    List<string> names = new List<string>() { "None" };
                    names.AddRange(s_EntityGroups.Select((other) => other.Name));
                    s_EntityGroupNames = names.ToArray();

                    Undo.RecordObject(aaSettings, "AddressableAssetSettings");

                    AddressableAssetGroup 
                        originGroup = entry.parentGroup,
                        targetGroup = GetIndexToGroup(currentGroupIdx);
                    
                    if (targetGroup != null)
                    {
                        aaSettings.MoveEntry(entry, targetGroup, false, false);

                        targetGroup.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, targetGroup, false, true);
                        aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, targetGroup, true, false);

                        CoreSystem.Logger.Log(Channel.Editor,
                            $"Asset({Path.GetFileName(entry.AssetPath)}) added to PrefabList({targetGroup.Name}) from {originGroup.Name}");
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorUtilities.Line();
            }
            else
            {
                GUILayout.BeginHorizontal();
                //if (s_ToggleMixed == null)
                //    s_ToggleMixed = new GUIStyle("ToggleMixed");
                //if (GUILayout.Toggle(false, s_AddressableAssetToggleText, s_ToggleMixed, GUILayout.ExpandWidth(false)))
                //    SetAaEntry(AddressableAssetSettingsDefaultObject.GetSettings(true), editor.targets, true);
                //EditorGUILayout.LabelField(addressableCount + " out of " + editor.targets.Length + " assets are addressable.");
                GUILayout.EndHorizontal();
            }
        }
    }
}
