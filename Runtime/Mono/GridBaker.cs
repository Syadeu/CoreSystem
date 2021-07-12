using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class GridBaker : MonoBehaviour
    {
        [SerializeField] private Vector3 m_GridCenter;
        [SerializeField] private Vector3 m_GridSize;
        [SerializeField] private float m_CellSize;

        [Space]
        [SerializeField] private bool m_EnableNavMesh;

        private int m_GridIdx;

        private void OnEnable()
        {
            m_GridIdx = GridManager.CreateGrid(new Bounds(m_GridCenter, m_GridSize), m_CellSize,
                m_EnableNavMesh);
        }
        private void OnDisable()
        {
            GridManager.RemoveGrid(m_GridIdx);
            m_GridIdx = -1;
        }
    }
}

