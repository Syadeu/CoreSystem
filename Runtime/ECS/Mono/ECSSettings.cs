using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.ECS
{
    public class ECSSettings : StaticSettingEntity<ECSSettings>
    {
#if UNITY_EDITOR
        [MenuItem("Syadeu/ECS/Edit Settings", priority = 1)]
        public static void MenuItem()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif
        [Header("Transform")]
        public float m_TrRoundOffset = .01f;

        [Header("Path Query")]
        public float m_PathNodeOffset = 2.5f;
        public int m_MaxQueries = 256;
        public int m_MaxPathSize = 1024;
        public int m_MaxIterations = 1024;
        public int m_MaxMapWidth = 10000;
        public bool m_StraightIfNotFound = true;
        public float m_ArrivalDistanceOffset = .5f;

        [Header("Path Agent")]
        public float m_AgentNodeOffset = .6f;
    }
}
