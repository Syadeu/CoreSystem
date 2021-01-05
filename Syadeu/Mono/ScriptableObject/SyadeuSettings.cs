using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono
{
    public class SyadeuSettings : StaticSettingEntity<SyadeuSettings>
    {
        // CoreSystem
        public bool m_VisualizeObjects = false;

        // PrefabManager
        public bool m_PMErrorAutoFix = true;
    }
}
