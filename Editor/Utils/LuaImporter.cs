using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

using Syadeu;
using Syadeu.Database;
using Syadeu.Mono;

namespace SyadeuEditor
{
    [ScriptedImporter(1, "lua")]
    internal sealed class LuaImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (!SyadeuSettings.Instance.m_EnableLua)
            {
                SyadeuSettings.Instance.m_EnableLua = true;
                EditorUtility.SetDirty(SyadeuSettings.Instance);
                AssetDatabase.SaveAssets();
            }

            var luaTxt = File.ReadAllText(ctx.assetPath); //Read as a string

            //Debug.Log("Import:" + ctx.assetPath);

            var assetsText = new TextAsset(luaTxt); // Convert to TextAsset, you can also write a LuaAsset class as the save object, but you must inherit the Object class

            ctx.AddObjectToAsset("main obj", assetsText); //This step and the next step seem to be repeated, but any missing step will report an exception

            ctx.SetMainObject(assetsText);
        }
    }
}
