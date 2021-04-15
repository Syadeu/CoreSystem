﻿using Syadeu.Mono.Creature;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Syadeu.Mono
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CreatureBrain : RecycleableMonobehaviour
    {
        internal int m_DataIdx;
        [SerializeField] private NavMeshAgent m_NavMeshAgent;

#if UNITY_EDITOR
        [Space]
        [SerializeField] private string m_CreatureName = null;
        [SerializeField] private string m_CreatureDescription = null;
#endif
        [Space]
        [Tooltip("활성화시, 카메라에 비치지 않으면 이동 메소드가 순간이동을 합니다")]
        public bool m_EnableCameraCull = true;
        [SerializeField] private float m_SamplePosDistance = .1f;

        [Space]
        [SerializeField] private UnityEvent m_OnCreated;
        [SerializeField] private UnityEvent<int> m_OnInitialize;
        [SerializeField] private UnityEvent<int> m_OnTerminate;

        public event Action<Vector3> onMove;

        public bool Initialized { get; private set; } = false;
        public bool IsOnGrid
        {
            get
            {
                return GridManager.HasGrid(CoreSystem.GetPosition(CoreSystem.GetTransform(transform)));
            }
        }
        public bool IsOnNavMesh => m_NavMeshAgent.isOnNavMesh;
        public bool IsMoving
        {
            get
            {
                if (m_NavMeshAgent.hasPath &&
                    m_NavMeshAgent.remainingDistance > .2f) return true;
                return false;
            }
        }

        internal override void Initialize()
        {
            if (m_NavMeshAgent == null) m_NavMeshAgent = GetComponent<NavMeshAgent>();

            m_OnInitialize?.Invoke(m_DataIdx);

            Initialized = true;

            base.Initialize();
        }
        public void ManualInitialize(int dataIdx)
        {
            if (Activated || Initialized)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"크리쳐 {name} 은 이미 사용등록이 완료되었으나 다시 등록하려합니다. \n" +
                    $"사용을 마친 후, Terminate() 메소드를 실행하세요.");
            }

            m_DataIdx = dataIdx;
            var set = CreatureSettings.Instance.GetPrivateSet(m_DataIdx);
            if (set == null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"데이터 인덱스 {m_DataIdx} 는 CreatureSettings 에 존재하지않습니다.\n" +
                    $"CreatureWindow를 먼저 키고 설정을 완료하세요.");
            }
            var recycleContainer = PrefabManager.Instance.InternalGetRecycleObject(set.m_PrefabIdx);
            if (recycleContainer == null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                    $"프리팹 인덱스 {set.m_PrefabIdx} 는 PrefabList 에 존재하지않습니다.");
            }
            recycleContainer.AddNewInstance(this);
            CreatureManager.Instance.m_Creatures.Add(this);

            Initialize();
        }

        public override void OnCreated()
        {
            m_OnCreated?.Invoke();
        }
        public override void OnTerminate()
        {
            m_OnTerminate?.Invoke(m_DataIdx);
            Initialized = false;
        }

        public ref GridManager.GridCell GetCurrentGridCell()
        {
            if (!IsOnGrid) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                $"{name}({transform.position}) 은 그리드 위에 존재하지 않습니다.");

            Vector3 pos = CoreSystem.GetPosition(CoreSystem.GetTransform(transform));
            ref GridManager.Grid grid = ref GridManager.GetGrid(pos);
            return ref grid.GetCell(pos);
        }

        #region Moves

        private CoreRoutine m_MoveRoutine;

        public void MoveTo(Vector3 worldPosition)
        {
            if (m_EnableCameraCull && !RenderManager.IsInCameraScreen(transform.position))
            {
                transform.position = worldPosition;
                return;
            }

            if (NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask) &&
                NavMesh.SamplePosition(worldPosition, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
            {
                m_NavMeshAgent.enabled = true;
                //m_NavMeshAgent.ResetPath();
                m_NavMeshAgent.SetDestination(worldPosition);
                m_MoveRoutine = CoreSystem.StartUnityUpdate(this, MoveToPointNavJob(worldPosition));
            }
            else
            {
                m_NavMeshAgent.enabled = false;
                m_MoveRoutine = CoreSystem.StartUnityUpdate(this, MoveToPointJob(worldPosition));
            }
        }
        public void MoveTo(GridManager.GridCell target) => MoveTo(target.Bounds.center);
        public void MoveTo(int gridIdx, int cellIdx)
        {
            Vector3 worldPos = GridManager.GetGrid(gridIdx).GetCell(cellIdx).Bounds.center;
            MoveTo(worldPos);
        }
        public void MoveTo(int2 gridIdxes) => MoveTo(gridIdxes.x, gridIdxes.y);
        public void MoveTo(Vector2Int gridIdxes) => MoveTo(gridIdxes.x, gridIdxes.y);

        public bool MoveToDirection(Vector3 direction)
        {
            if (NavMesh.SamplePosition(transform.position + direction, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask) &&
                NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
            {
                if (m_MoveRoutine.IsRunning) CoreSystem.RemoveUnityUpdate(m_MoveRoutine);
                if (m_NavMeshAgent.isOnNavMesh && (
                    m_NavMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                    m_NavMeshAgent.pathStatus == NavMeshPathStatus.PathPartial))
                {
                    m_NavMeshAgent.ResetPath();
                }

                m_NavMeshAgent.enabled = true;
                m_NavMeshAgent.Move(direction);

                onMove?.Invoke(transform.position + direction);

                return true;
            }
            else
            {
                m_NavMeshAgent.enabled = false;
                transform.position += direction * .6f;

                onMove?.Invoke(transform.position);

                return false;
            }
        }
        private IEnumerator MoveToPointNavJob(Vector3 worldPosition)
        {
            float sqr = (worldPosition - transform.position).sqrMagnitude;

            while (sqr > .25f)
            {
                if (m_EnableCameraCull && !RenderManager.IsInCameraScreen(transform.position))
                {
                    //m_NavMeshAgent.ResetPath();
                    transform.position = worldPosition;
                    onMove?.Invoke(worldPosition);
                    yield break;
                }

                if (m_NavMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                    !NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
                {
                    MoveTo(worldPosition);
                    yield break;
                }

                sqr = (worldPosition - transform.position).sqrMagnitude;
                onMove?.Invoke(transform.position);

                yield return null;
            }
        }
        private IEnumerator MoveToPointJob(Vector3 worldPosition)
        {
            float sqr = (worldPosition - transform.position).sqrMagnitude;
            Vector3 targetAxis;

            while (sqr > .25f)
            {
                if (m_EnableCameraCull && !RenderManager.IsInCameraScreen(transform.position))
                {
                    transform.position = worldPosition;
                    onMove?.Invoke(worldPosition);
                    yield break;
                }

                if (NavMesh.SamplePosition(worldPosition, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask) &&
                    NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
                {
                    MoveTo(worldPosition);
                    yield break;
                }

                sqr = (worldPosition - transform.position).sqrMagnitude;
                targetAxis = (worldPosition - transform.position).normalized * m_NavMeshAgent.speed * 1.87f;

                transform.position = Vector3.Lerp(transform.position, transform.position + targetAxis, Time.deltaTime * m_NavMeshAgent.angularSpeed);

                onMove?.Invoke(transform.position);

                yield return null;
            }
        }

        #endregion
    }


}