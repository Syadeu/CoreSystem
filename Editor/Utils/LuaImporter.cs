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

namespace SyadeuEditor
{
    [ScriptedImporter(1, "lua")]
    internal sealed class LuaImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var luaTxt = File.ReadAllText(ctx.assetPath); //Read as a string

            //Debug.Log("Import:" + ctx.assetPath);

            var assetsText = new TextAsset(luaTxt); // Convert to TextAsset, you can also write a LuaAsset class as the save object, but you must inherit the Object class

            ctx.AddObjectToAsset("main obj", assetsText); //This step and the next step seem to be repeated, but any missing step will report an exception

            ctx.SetMainObject(assetsText);
        }
    }
    internal sealed class LuaPostprocessor : AssetPostprocessor
    {
        //Processing after resource loading
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                if (str.EndsWith(".lua"))
                {
                    //Debug.Log("LuaPostprocessor:" + str);

                    var lua_obj = AssetDatabase.LoadAssetAtPath<Object>(str);

                    AssetDatabase.SetLabels(lua_obj, new string[] { "lua" });
                }
            }

            foreach (string str in deletedAssets)
            {
                //Debug.Log("Deleted Asset: " + str);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                //Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
            }
        }

        void OnPreprocessAsset() //Preprocessing before resource loading
        {
            if (assetImporter.assetPath.EndsWith(".lua"))
            {
                //Debug.Log("LuaPreprocessor:" + assetImporter.assetPath);
            }
        }
    }
}
