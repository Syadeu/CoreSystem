using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.Platformer
{
    public abstract class PFCharacterControllerBase : MonoBehaviour
    {
        [SerializeField] private Transform m_OverrideTransform = null;

        [Space]
        [Header("Input Control")]
        [SerializeField] private InputAction m_MoveAxis;
        [SerializeField] private float m_MoveOffset = .1f;
        [SerializeField] private float m_MoveSpeed = 8f;

        private float3 m_TargetPosition = float3.zero;

        private Transform TargetTransform
        {
            get
            {
                if (m_OverrideTransform == null) return transform;
                return m_OverrideTransform;
            }
        }
        public float3 TargetPosition
        {
            get => m_TargetPosition;
            set
            {
                m_TargetPosition = value;
            }
        }

        private void OnEnable()
        {
            m_MoveAxis.Enable();
        }
        private void OnDisable()
        {
            m_MoveAxis.Disable();
        }


        private IEnumerator Start()
        {
            

            while (true)
            {
                if (m_MoveAxis.IsPressed())
                {
                    Vector2 axis = m_MoveAxis.ReadValue<Vector2>();
                    axis *= m_MoveOffset;

                    m_TargetPosition.x += axis.x;
                    //m_TargetPosition.y += axis.y;
                }

                TargetTransform.position
                        = math.lerp(TargetTransform.position, TargetPosition, Time.deltaTime * m_MoveSpeed);

                yield return null;
            }
        }
    }
}