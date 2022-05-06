//using Unity.CompilationPipeline.Common.Diagnostics;
//using Unity.CompilationPipeline.Common.ILPostProcessing;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Syadeu.Collections;
using Syadeu.Presentation.Components;
using SyadeuEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Syadeu.Presentation.CodeGen
{
    public sealed class CodeGeneratorMenuItem : SetupWizardMenuItem
    {
        public override string Name => "Code Generator";
        public override int Order => 9999;

        ComponentExportAttribute[] m_ComponentExporters;
        string[] m_ClassNames, m_ClassPaths;
        MonoScript[] m_ClassScripts;

        public override void OnInitialize()
        {
            var iter = TypeHelper.GetTypesIter(
                t => !t.IsAbstract && !t.IsInterface && 
                    TypeHelper.TypeOf<ObjectBase>.Type.IsAssignableFrom(t) &&
                    t.GetCustomAttribute<ComponentExportAttribute>() != null);

            UnityEditor.Compilation.Assembly[] asems = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();

            m_ComponentExporters = new ComponentExportAttribute[iter.Count()];
            m_ClassNames = new string[m_ComponentExporters.Length];
            m_ClassPaths = new string[m_ComponentExporters.Length];
            m_ClassScripts = new MonoScript[m_ComponentExporters.Length];
            {
                int i = 0;
                foreach (var item in iter)
                {
                    m_ComponentExporters[i] = item.GetCustomAttribute<ComponentExportAttribute>();
                    m_ClassNames[i] = TypeHelper.ToString(item);

                    //var temp = ModuleDefinition.ReadModule(item.Module.FullyQualifiedName);

                    //var typedef = temp.GetType(item.AssemblyQualifiedName);

                    //$"{item.Module.FullyQualifiedName} :: {item.Assembly.Location}".ToLog();
                    string scriptPath = ScriptUtilities.GetScriptPath(m_ClassNames[i]);
                    if (scriptPath != null)
                    {
                        m_ClassPaths[i] = scriptPath;
                        m_ClassScripts[i] = AssetDatabase.LoadAssetAtPath<MonoScript>(m_ClassPaths[i]);
                    }
                    else
                    {
                        m_ClassPaths[i] = string.Empty;
                        m_ClassScripts[i] = null;
                    }

                    i++;
                }
            }

        }
        public override void OnGUI()
        {
            for (int i = 0; i < m_ClassNames.Length; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(m_ClassNames[i]);

                    if (GUILayout.Button("test"))
                    {
                        $"{(m_ClassScripts[i] == null ? "NotFOUND" : m_ClassScripts[i].name)}".ToLog();
                    }
                }
            }
        }
        public override bool Predicate()
        {
            return true;
        }
    }
}
