using System.Collections.Generic;
using System.Linq;

using UnityEditor;

namespace SyadeuEditor
{
    public static class ScriptUtilites
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
    }
}
