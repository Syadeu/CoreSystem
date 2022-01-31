//using Unity.CompilationPipeline.Common.Diagnostics;
//using Unity.CompilationPipeline.Common.ILPostProcessing;

using System.IO;

namespace Syadeu.Presentation.CodeGen
{
    // https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.html
    public sealed class TEstProcc : UnityEditor.AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            //var objs = paths.Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>);
            foreach (var item in paths)
            {
                if (Path.GetExtension(item) != ".cs") continue;

                string scr = File.ReadAllText(item);
                if (!scr.Contains("[GuidMarker]")) continue;

                UnityEngine.Debug.Log($"{item} has guid marker");
            }

            //UnityEngine.Debug.Log("OnWillSaveAssets");
            //foreach (string path in paths)
            //    UnityEngine.Debug.Log(path);
            return paths;
        }
    }
}
