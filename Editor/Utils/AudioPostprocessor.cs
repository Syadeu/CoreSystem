using UnityEditor;

namespace SyadeuEditor
{
#if CORESYSTEM_UNITYAUDIO
    internal sealed class AudioPostprocessor : AssetPostprocessor
    {
        //Processing after resource loading
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                //if (str.EndsWith(".lua"))
                //{
                //    //Debug.Log("LuaPostprocessor:" + str);

                //    var lua_obj = AssetDatabase.LoadAssetAtPath<Object>(str);

                //    AssetDatabase.SetLabels(lua_obj, new string[] { "lua" });
                //}
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
            //if (assetImporter.assetPath.EndsWith(".lua"))
            //{
            //    //Debug.Log("LuaPreprocessor:" + assetImporter.assetPath);
            //}
        }
    }
#endif
}
