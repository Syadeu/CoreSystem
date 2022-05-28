#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
#endif

using Syadeu.Collections.Editor;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    internal sealed class GraphicsSetupMenuItem : SetupWizardMenuItem
    {
        public override string Name => "Graphics";
        public override int Order => -9996;

        Syadeu.Presentation.Render.RenderSettings Settings => Syadeu.Presentation.Render.RenderSettings.Instance;

        const string
            c_MaterialLabel = "Material",
            c_ComputeShaderLabel = "ComputeShader",
            c_ShaderLabel = "Shader";

        private Material[] m_FoundMaterials;
        private ComputeShader[] m_FoundComputeShaders;
        private Shader[] m_FoundShaders;

        public override void OnInitialize()
        {
            try
            {
                m_FoundMaterials = AssetDatabase
                    .FindAssets($"l: {c_MaterialLabel} t:material")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<Material>)
                    .ToArray();
            }
            catch (Exception)
            {
                m_FoundMaterials = Array.Empty<Material>();
            }
            try
            {
                m_FoundComputeShaders = AssetDatabase
                    .FindAssets($"l: {c_ComputeShaderLabel} t:computeshader")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<ComputeShader>)
                    .ToArray();
            }
            catch (Exception)
            {
                m_FoundComputeShaders = Array.Empty<ComputeShader>();
            }
            //Array.Sort(Settings.m_ComputeShaders);
            //Array.Sort(m_FoundComputeShaders);

            try
            {
                m_FoundShaders = AssetDatabase
                    .FindAssets($"l: {c_ShaderLabel} t:shader")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<Shader>)
                    .ToArray();
            }
            catch (Exception)
            {
                m_FoundShaders = Array.Empty<Shader>();
            }
            //Array.Sort(Settings.m_Shaders);
            //Array.Sort(m_FoundShaders);
        }
        public override bool Predicate()
        {
            #region Material

            if (m_FoundMaterials.Length != Settings.m_Materials.Length)
            {
                return false;
            }
            for (int i = 0; i < m_FoundMaterials.Length; i++)
            {
                if (m_FoundMaterials[i] != Settings.m_Materials[i]) return false;
            }

            #endregion

            #region Compute Shaders

            if (m_FoundComputeShaders.Length != Settings.m_ComputeShaders.Length)
            {
                return false;
            }
            for (int i = 0; i < m_FoundComputeShaders.Length; i++)
            {
                if (m_FoundComputeShaders[i] != Settings.m_ComputeShaders[i]) return false;
            }

            #endregion

            #region Shaders

            if (m_FoundShaders.Length != Settings.m_Shaders.Length)
            {
                return false;
            }
            for (int i = 0; i < m_FoundShaders.Length; i++)
            {
                if (m_FoundShaders[i] != Settings.m_Shaders[i]) return false;
            }

            #endregion

            return true;
        }
        public override void OnGUI()
        {
            if (!Predicate())
            {
                EditorGUILayout.LabelField("Error.");

                if (GUILayout.Button("Fix"))
                {
                    Settings.m_Materials = m_FoundMaterials;
                    Settings.m_ComputeShaders = m_FoundComputeShaders;
                    Settings.m_Shaders = m_FoundShaders;

                    EditorUtility.SetDirty(Settings);
                }
            }

            using (new CoreGUI.BoxBlock(Color.black))
            {
                CoreGUI.Label("Nominal", TextAnchor.MiddleCenter);
            }
        }
    }
}
