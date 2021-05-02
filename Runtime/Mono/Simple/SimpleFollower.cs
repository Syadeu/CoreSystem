using System.Collections;
using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class SimpleFollower : MonoBehaviour
    {
        [SerializeField] private Transform m_Target;
        [SerializeField] private Vector3 m_Offset;
        [SerializeField] private float m_Speed;

        private Coroutine m_UpdateCor = null;
        private Vector3 m_TargetPosition = Vector3.zero;

        public Vector3 Target
        {
            get
            {
                if (m_Target == null)
                {
                    return m_TargetPosition;
                }

                m_TargetPosition = CoreSystem.GetPosition(m_Target);
                return m_TargetPosition;
            }
        }
        public Vector3 Offset => m_Offset;
        public float Speed => m_Speed;

        private void OnEnable()
        {
            m_UpdateCor = StartCoroutine(Updater());
        }
        private IEnumerator Updater()
        {
            WaitUntil targetNotNull = new WaitUntil(() => m_Target != null);

        Try1:
            yield return targetNotNull;

            while (m_Target != null)
            {
                transform.position
                    = Vector3.Lerp(transform.position, Target + m_Offset, m_Speed * Time.deltaTime);

                yield return null;
            }

            goto Try1;
        }
        private void OnDisable()
        {
            if (m_UpdateCor != null) StopCoroutine(m_UpdateCor);
            m_UpdateCor = null;
        }

        public void SetTarget(Transform tr) => m_Target = tr;
        public void SetTarget(Vector3 target)
        {
            m_Target = null;
            m_TargetPosition = target;
        }
        public void SetOffset(Vector3 offset) => m_Offset = offset;
        public void SetSpeed(float speed) => m_Speed = speed;
    }
}