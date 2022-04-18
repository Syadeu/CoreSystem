using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;

namespace SyadeuEditor
{
    public static class ScriptUtilities
    {
        public static string GetScriptPath(string typeName)
        {
            const string c_Format = "{0} t:script";

            IEnumerable<string> scriptPath = AssetDatabase.FindAssets(string.Format(c_Format, typeName)).Select(AssetDatabase.GUIDToAssetPath);
            if (scriptPath.Any())
            {
                return scriptPath.First();
            }
            return null;
        }
        public static string GetCallerScriptPath()
        {
            return new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileName();
        }

        public static MonoScript FindScriptFromClassName(string className)
        {
            var scriptGUIDs = AssetDatabase.FindAssets($"t:script {className}");

            if (scriptGUIDs.Length == 0)
                return null;

            foreach (var scriptGUID in scriptGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(scriptGUID);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (script != null && string.Equals(className, Path.GetFileNameWithoutExtension(assetPath), StringComparison.OrdinalIgnoreCase))
                    return script;
            }

            return null;
        }
    }
}
