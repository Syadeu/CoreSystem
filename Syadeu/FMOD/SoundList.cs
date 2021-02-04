using System.Collections.Generic;

using UnityEngine;
using System;
using FMODUnity;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.FMOD
{
    [CreateAssetMenu(fileName = "newSoundlist", menuName = "Syadeu/FMOD/Soundlist")]
    public class SoundList : ScriptableObject
    {
        [Serializable]
        public class EventClips
        {
            public int type;
            [EventRef]
            public string Event = "";
            public EmitterGameEvent PlayEvent = EmitterGameEvent.None;
            public EmitterGameEvent StopEvent = EmitterGameEvent.None;
            public bool AllowFadeout = true;
            public bool TriggerOnce = false;
            public bool Preload = false;
            public ParamRef[] Params = new ParamRef[0];
            public bool OverrideAttenuation = false;
            public float OverrideMinDistance = -1.0f;
            public float OverrideMaxDistance = -1.0f;
        }
        [Serializable]
        public class FInput : ICloneable
        {
            public string name = "이름을 입력하세요";
            public int index = 0;

            public int clipCount;
            public List<EventClips> eventClips = new List<EventClips>();

            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        public string listName = "이름을 입력하세요";
        public int listIndex;
        //public int soundListType;
        public List<FInput> fSounds = new List<FInput>();

#if UNITY_EDITOR
        [MenuItem("Syadeu/FMOD/Create New Soundlist", priority = 3)]
        public static void CreateSoundlist()
        {
            SoundList list = CreateInstance<SoundList>();
            list.name = "New Soundlist";

            if (!Directory.Exists("Assets/Resources/Syadeu"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Syadeu");
            }
            if (!Directory.Exists("Assets/Resources/Syadeu/Soundlist"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Syadeu", "Soundlist");
            }
            AssetDatabase.CreateAsset(list, "Assets/Resources/Syadeu/Soundlist/New Soundlist.asset");

            Selection.activeObject = list;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif
    }
}
