using Syadeu;
using System.IO;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

#if UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SyadeuEditor
{
    public sealed class CoreSystemDataBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            $"{report.summary.outputPath} : is output".ToLog();
            Copy($"{Application.dataPath}/../CoreSystem", $"{Path.GetDirectoryName(report.summary.outputPath)}/CoreSystem");

            void Copy(string sourceDir, string targetDir)
            {
                if (!Directory.Exists(targetDir))
                {
                    $"create directory {targetDir}".ToLog();
                    Directory.CreateDirectory(targetDir);
                }
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    if (File.Exists(Path.Combine(targetDir, Path.GetFileName(file))))
                    {
                        System.DateTime dest = File.GetLastWriteTimeUtc(Path.Combine(targetDir, Path.GetFileName(file)));
                        System.DateTime origin = File.GetLastWriteTimeUtc(file);
                        if (dest.Equals(origin)) continue;

                        File.Delete(Path.Combine(targetDir, Path.GetFileName(file)));
                    }
                    File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
                }

                foreach (var directory in Directory.GetDirectories(sourceDir))
                    Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
            }
        }
    }

#if UNITY_ADDRESSABLES
    public sealed class PrefabBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            AddressableAssetSettings.BuildPlayerContent();
        }
    }
#endif
}
