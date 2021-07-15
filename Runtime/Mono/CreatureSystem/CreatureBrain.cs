using Syadeu.Presentation;
using Syadeu.Database;
using Syadeu.Database.Lua;
using Syadeu.Mono.Creature;
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
    /// <summary>
    /// 하위 컴포넌트들은 <seealso cref="CreatureEntity"/> 를 참조하면 자동으로 Initialize 됨.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))] [LuaArgumentType]
    public sealed class CreatureBrain : RecycleableMonobehaviour
    {
        internal Hash m_DataHash;
        internal DataGameObject m_DataObject;

        [SerializeField] private NavMeshAgent m_NavMeshAgent;

        [Space]
        [Tooltip("활성화시, 카메라에 비치지 않으면 이동 메소드가 순간이동을 합니다")]
        //[Obsolete] public bool m_EnableCameraCull = true;
        [SerializeField] private float m_SamplePosDistance = .25f;

        private CreatureEntity[] m_Childs = null;
        private CreatureBrainProxy m_Proxy = null;
        //private Hash m_Hash;

        public override string DisplayName => m_DataObject.GetComponent<CreatureInfoDataComponent>().m_CreatureInfo.m_Name;
        public override bool InitializeOnCall => false;

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
                if (m_NavMeshAgent.desiredVelocity.magnitude > 0 &&
                    m_NavMeshAgent.remainingDistance > .2f) return true;
                return false;
            }
        }
        public Vector3 Direction => m_NavMeshAgent.desiredVelocity;
        //public Vector3 Speed => m_NavMeshAgent.;
        //public Hash Hash => m_Hash;

        public bool HasInventory => Inventory != null;
        public CreatureInventory Inventory { get; private set; }
        public CreatureStat Stat { get; private set; }

        internal CreatureBrainProxy Proxy
        {
            get
            {
                if (m_Proxy == null) m_Proxy = new CreatureBrainProxy(this);
                return m_Proxy;
            }
        }

        protected override void OnCreated()
        {
            m_SharedPath = new NavMeshPath();
            //m_Hash = Hash.NewHash();
            m_Childs = GetComponentsInChildren<CreatureEntity>();

            if (m_NavMeshAgent == null) m_NavMeshAgent = GetComponentInChildren<NavMeshAgent>();

            for (int i = 0; i < m_Childs.Length; i++)
            {
                if (m_Childs[i] is CreatureInventory inventory) Inventory = inventory;
                else if (m_Childs[i] is CreatureStat stat) Stat = stat;

                m_Childs[i].InternalOnCreated();
            }

            //m_OnCreated?.Invoke();
        }
        protected override void OnInitialize()
        {
            //m_OnInitialize?.Invoke(m_DataIdx);
            for (int i = 0; i < m_Childs.Length; i++)
            {
                m_Childs[i].InternalInitialize(this, m_DataObject);
            }
            for (int i = 0; i < m_Childs.Length; i++)
            {
                m_Childs[i].InternalOnStart();
            }

            //PresentationSystem<RenderSystem>.System.AddObserver(this);

            m_NavMeshAgent.enabled = false;
            m_NavMeshAgent.enabled = true;

            Initialized = true;
        }
        protected override void OnTerminate()
        {
            //m_OnTerminate?.Invoke(m_DataIdx);

            //var set = CreatureManager.GetCreatureSet(m_DataIdx);
            //set.m_SpawnRanges[m_SpawnPointIdx].m_InstanceCount--;

            //transform.position = INIT_POSITION;

            for (int i = 0; i < m_Childs.Length; i++)
            {
                m_Childs[i].InternalOnTerminate();
            }

            //PresentationSystem<RenderSystem>.System.RemoveObserver(this);
            //CreatureManager.Instance.Creatures.Remove(this);
            //m_IsSpawnedFromManager = false;

            m_NavMeshAgent.enabled = false;

            Initialized = false;
        }
        //protected virtual void OnDestroy()
        //{
        //    //if (CreatureManager.HasInstance)
        //    //{
        //    //    var set = CreatureManager.GetCreatureSet(m_DataIdx);
        //    //    set.m_SpawnRanges[m_SpawnPointIdx].m_InstanceCount--;
        //    //    CreatureManager.Instance.Creatures.Remove(this);
        //    //}
        //    //PresentationSystem<RenderSystem>.System.RemoveObserver(this);
        //}

        /// <summary>
        /// 런타임 중 추가된 자식 CreatureEntity 를 초기화 하기 위한 함수입니다.
        /// </summary>
        /// <param name="entity"></param>
        public void InitializeCreatureEntity(CreatureEntity entity)
        {
            if (!CoreSystem.IsThisMainthread()) throw new CoreSystemThreadSafeMethodException("InitializeCreatureEntity");

            for (int i = 0; i < m_Childs.Length; i++)
            {
                if (m_Childs[i].Equals(entity)) throw new Exception();
            }
            var temp = m_Childs.ToList();
            temp.Add(entity);
            m_Childs = temp.ToArray();

            if (entity is CreatureInventory inventory) Inventory = inventory;
            else if (entity is CreatureStat stat) Stat = stat;

            entity.InternalInitialize(this, m_DataObject);
        }
        //protected virtual void OnDestroy()
        //{
        //    RenderManager.RemoveObserver(this);
        //}

        //public void OnVisible()
        //{
        //    IsVisible = true;
        //    for (int i = 0; i < m_Childs.Length; i++)
        //    {
        //        m_Childs[i].OnVisible();
        //    }
        //    LuaCreatureUtils.OnVisible?.Invoke(Proxy);
        //}
        //public void OnInvisible()
        //{
        //    IsVisible = false;
        //    for (int i = 0; i < m_Childs.Length; i++)
        //    {
        //        m_Childs[i].OnInvisible();
        //    }
        //    LuaCreatureUtils.OnInvisible?.Invoke(Proxy);
        //}

        public int2 m_CachedCurrentGridIdxes = -1;
        public Guid GetCurrentGrid()
        {
            if (!IsOnGrid) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                $"{name}({transform.position}) 은 그리드 위에 존재하지 않습니다.");

            Vector3 pos = CoreSystem.GetPosition(CoreSystem.GetTransform(transform));
            ref GridManager.Grid grid = ref GridManager.GetGrid(pos);

            return grid.Guid;
        }
        public ref GridManager.GridCell GetCurrentGridCell()
        {
            if (!IsOnGrid) throw new CoreSystemException(CoreSystemExceptionFlag.Mono,
                $"{name}({transform.position}) 은 그리드 위에 존재하지 않습니다.");

            Vector3 pos = CoreSystem.GetPosition(CoreSystem.GetTransform(transform));
            ref GridManager.Grid grid = ref GridManager.GetGrid(pos);
            ref GridManager.GridCell cell = ref grid.GetCell(pos);
            m_CachedCurrentGridIdxes = cell.Idxes;

            return ref cell;
        }

        #region Moves

        private CoreRoutine m_MoveRoutine;
        private NavMeshPath m_SharedPath;

        public Vector3[] CalculatePath(Vector3 target)
        {
            m_NavMeshAgent.CalculatePath(target, m_SharedPath);
            return m_SharedPath.corners;
        }
        public bool MoveTo(Vector3 worldPosition, Action onCompleted = null, bool force = false)
        {
            //if (!PresentationSystem<RenderSystem>.System.IsInCameraScreen(transform.position))
            //{
            //    transform.position = worldPosition;
            //    return true;
            //}

            if (NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask) &&
                NavMesh.SamplePosition(worldPosition, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
            {
                m_NavMeshAgent.enabled = true;
                //m_NavMeshAgent.ResetPath();
                if (force)
                {
                    if (!m_NavMeshAgent.CalculatePath(worldPosition, m_SharedPath))
                    {
                        return false;
                    }
                    m_NavMeshAgent.SetPath(m_SharedPath);
                }
                else
                {
                    if (!m_NavMeshAgent.SetDestination(worldPosition))
                    {
                        return false;
                    }
                }

                m_MoveRoutine = CoreSystem.StartUnityUpdate(this, MoveToPointNavJob(worldPosition, onCompleted));
            }
            else
            {
                m_NavMeshAgent.enabled = false;
                m_MoveRoutine = CoreSystem.StartUnityUpdate(this, MoveToPointJob(worldPosition));
            }

            return true;
        }
        public void MoveTo(GridManager.GridCell target, Action onCompleted = null) => MoveTo(target.Bounds.center, onCompleted);
        public void MoveTo(int gridIdx, int cellIdx, Action onCompleted = null)
        {
            Vector3 worldPos = GridManager.GetGrid(gridIdx).GetCell(cellIdx).Bounds.center;
            MoveTo(worldPos, onCompleted);
        }
        public void MoveTo(int2 gridIdxes, Action onCompleted = null) => MoveTo(gridIdxes.x, gridIdxes.y, onCompleted);
        public void MoveTo(Vector2Int gridIdxes, Action onCompleted = null) => MoveTo(gridIdxes.x, gridIdxes.y, onCompleted);

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

                return true;
            }
            else
            {
                m_NavMeshAgent.enabled = false;
                transform.position += direction * .6f;

                return false;
            }
        }
        private IEnumerator MoveToPointNavJob(Vector3 worldPosition, Action onCompleted)
        {
            float sqr = (worldPosition - transform.position).sqrMagnitude;

            while (sqr > .25f)
            {
                //if (m_EnableCameraCull && !PresentationSystem<RenderSystem>.System.IsInCameraScreen(transform.position))
                //{
                //    //m_NavMeshAgent.ResetPath();
                //    transform.position = worldPosition;
                //    break;
                //}

                if (m_NavMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                    !NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
                {
                    MoveTo(worldPosition);
                    yield break;
                }

                sqr = (worldPosition - transform.position).sqrMagnitude;
                m_DataObject.transform.SynchronizeWithProxy();
                yield return null;
            }

            onCompleted?.Invoke();
        }
        private IEnumerator MoveToPointJob(Vector3 worldPosition)
        {
            float sqr = (worldPosition - transform.position).sqrMagnitude;
            Vector3 targetAxis;

            while (sqr > .25f)
            {
                //if (m_EnableCameraCull && !PresentationSystem<RenderSystem>.System.IsInCameraScreen(transform.position))
                //{
                //    transform.position = worldPosition;
                //    yield break;
                //}

                if (NavMesh.SamplePosition(worldPosition, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask) &&
                    NavMesh.SamplePosition(transform.position, out _, m_SamplePosDistance, m_NavMeshAgent.areaMask))
                {
                    MoveTo(worldPosition);
                    yield break;
                }

                sqr = (worldPosition - transform.position).sqrMagnitude;
                targetAxis = (worldPosition - transform.position).normalized * m_NavMeshAgent.speed * 1.87f;

                transform.position = Vector3.Lerp(transform.position, transform.position + targetAxis, Time.deltaTime * m_NavMeshAgent.angularSpeed);
                m_DataObject.transform.SynchronizeWithProxy();
                yield return null;
            }
        }

        #endregion
    }
}
