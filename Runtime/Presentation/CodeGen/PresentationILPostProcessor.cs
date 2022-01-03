using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Burst.CompilerServices;
using UnityEditor.Compilation;
using UnityEditor;
//using Unity.CompilationPipeline.Common.Diagnostics;
//using Unity.CompilationPipeline.Common.ILPostProcessing;

using Unity.Burst.Editor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Build.Pipeline;
using UnityEditor.Il2Cpp;
using Unity.Burst;
using UnityEngine;

namespace Syadeu.Presentation.CodeGen
{
    // https://forum.unity.com/threads/how-does-unity-do-codegen-and-why-cant-i-do-it-myself.853867/
    // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Scripting/ScriptCompilation/UnityCodeGenHelpers.cs#L36

    // https://docs.unity3d.com/ScriptReference/SessionState.html
    internal class PresentationConstantHashGenerator
    {
        //[MenuItem("AssemblyBuilder Example/Build Assembly Async")]
        public static void BuildAssemblyAsync()
        {
            BuildAssembly(false);
        }

        private void test()
        {
            CompilationPipeline.assemblyCompilationFinished += CompilationPipeline_assemblyCompilationFinished;
        }

        // https://docs.unity3d.com/ScriptReference/Compilation.CompilationPipeline-assemblyCompilationFinished.html
        private void CompilationPipeline_assemblyCompilationFinished(string outputAssemblyPath, CompilerMessage[] arg2)
        {
            //throw new NotImplementedException();
        }

        static void BuildAssembly(bool wait)
        {
            var sources = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                [0].sourceFiles;

            var scripts = new[] { "Temp/MyAssembly/MyScript1.cs", "Temp/MyAssembly/MyScript2.cs" };
            var outputAssembly = "Temp/MyAssembly/MyAssembly.dll";
            var assemblyProjectPath = "Assets/MyAssembly.dll";

            Directory.CreateDirectory("Temp/MyAssembly");

            // Create scripts
            foreach (var scriptPath in scripts)
            {
                var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                File.WriteAllText(scriptPath, string.Format("using UnityEngine; public class {0} : MonoBehaviour {{ void Start() {{ Debug.Log(\"{0}\"); }} }}", scriptName));
            }

            var assemblyBuilder = new AssemblyBuilder(outputAssembly, scripts);

            // Exclude a reference to the copy of the assembly in the Assets folder, if any.
            assemblyBuilder.excludeReferences = new string[] { assemblyProjectPath };

            // Called on main thread
            assemblyBuilder.buildStarted += delegate (string assemblyPath)
            {
                Debug.LogFormat("Assembly build started for {0}", assemblyPath);
            };

            // Called on main thread
            assemblyBuilder.buildFinished += delegate (string assemblyPath, CompilerMessage[] compilerMessages)
            {
                var errorCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                var warningCount = compilerMessages.Count(m => m.type == CompilerMessageType.Warning);

                Debug.LogFormat("Assembly build finished for {0}", assemblyPath);
                Debug.LogFormat("Warnings: {0} - Errors: {0}", errorCount, warningCount);

                if (errorCount == 0)
                {
                    File.Copy(outputAssembly, assemblyProjectPath, true);
                    AssetDatabase.ImportAsset(assemblyProjectPath);
                }
            };

            // Start build of assembly
            if (!assemblyBuilder.Build())
            {
                Debug.LogErrorFormat("Failed to start build of assembly {0}!", assemblyBuilder.assemblyPath);
                return;
            }

            if (wait)
            {
                while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
                    System.Threading.Thread.Sleep(10);
            }


        }
    }
}
