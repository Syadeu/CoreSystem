using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGCameraControlProvider : MonoBehaviour, AxisState.IInputAxisProvider
    {
        [SerializeField] private InputAction m_MoveAxis;

        [Space]
        [SerializeField] private InputAction m_RotateRight;
        [SerializeField] private InputAction m_RotateLeft;

        private Vector2 Axis => m_MoveAxis.enabled ? m_MoveAxis.ReadValue<Vector2>() : Vector2.zero;

        private void OnEnable()
        {
            if (m_MoveAxis != null) m_MoveAxis.Enable();

        }
        private void OnDisable()
        {
            if (m_MoveAxis != null) m_MoveAxis.Disable();
        }

        float AxisState.IInputAxisProvider.GetAxisValue(int axis)
        {
            switch (axis)
            {
                case 0: return Axis.x;
                case 1: return Axis.y;
            }

            return 0;
        }
    }
}
