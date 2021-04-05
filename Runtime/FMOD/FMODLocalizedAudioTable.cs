
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.FMOD
{
#if CORESYSTEM_FMOD
    public sealed class FMODLocalizedAudioTable : StaticSettingEntity<FMODLocalizedAudioTable>
    {
        [SerializeField] private AudioTableRoot[] m_AudioTableDatas;
        public static AudioTableRoot[] AudioTableDatas { get { return Instance.m_AudioTableDatas; } set { Instance.m_AudioTableDatas = value; } }

#if UNITY_EDITOR
        [MenuItem("Syadeu/FMOD/Edit Localized Audio Table", priority = 3)]
        public static void EditSettings()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif
    }
#endif
}