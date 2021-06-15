using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Syadeu;

#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#elif CORESYSTEM_UNITYAUDIO
using Syadeu.Mono.Audio;
#endif

namespace SyadeuEditor
{
    public sealed class ItemDesigner : EditorWindow
    {

    }
}