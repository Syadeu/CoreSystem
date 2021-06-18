using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Syadeu;
using System.IO;

#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
using Syadeu.Mono.Audio;
#endif

namespace SyadeuEditor
{
    public sealed class ItemDesigner : EditorWindow
    {
        private const string c_DataPath = "CoreSystem/Data/Item";
        public static string DataPath => $"{Application.dataPath}/../{c_DataPath}";

        private void OnEnable()
        {
            //if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);

            //Directory.g
        }
        private void OnGUI()
        {
            
        }
    }
}