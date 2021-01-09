using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Syadeu.Mono
{
    public class SyadeuSettings : StaticSettingEntity<SyadeuSettings>
    {
        // Global System
        public bool m_VisualizeObjects = false;

        // PrefabManager
        public bool m_PMErrorAutoFix = true;

        // FMODManager
        public int m_FMODMemoryBlock = 512;

        // RenderManager
        public Vector3 m_ScreenOffset = new Vector3(.4f, .5f, 1);
    }
}
 