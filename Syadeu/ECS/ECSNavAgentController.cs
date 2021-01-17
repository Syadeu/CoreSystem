using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine.Jobs;
using System.Collections.Generic;
using System.Linq;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    [DisallowMultipleComponent]
    public class ECSNavAgentController : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent m_NavMeshAgent;
        [SerializeField] private int typeID;

        public Transform target;
        public float moveSpeed = 3;

        private EntityManager entityManager;
        private Entity Entity { get; set; }

        //public void RequestPath(Vector3 target)
        //{
        //    //ECSNavQuerySystem.RequestPath(Entity, target, 1);
        //}

        private float random()
        {
            return UnityEngine.Random.Range(1, 100);
        }
        PathfinderID id;
        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            id = ECSPathQuerySystem.RegisterPathfinder(transform, typeID);

            //for (int i = 0; i < 100; i++)
            //{
            //    ECSPathQuerySystem.SchedulePath(id,
            //        new Vector3(random(), 0, random()));
            //}

            //EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            //Entity = EntityManager.CreateEntity(
            //    typeof(ECSNavAgentTransform), typeof(ECSNavAgentPathfinder),
            //    typeof(ECSNavAgent));
            //EntityManager.SetName(Entity, $"{name:: ECSNavAgent}");
            //EntityManager.AddComponentData(Entity, new ECSNavAgentTransform
            //{
            //    position = transform.position,
            //    rotation = transform.rotation
            //});
            //EntityManager.AddComponentData(Entity, new ECSNavAgent
            //{
            //    height = m_NavMeshAgent.height,
            //    radius = m_NavMeshAgent.radius
            //});
            //EntityManager.AddComponentData(Entity, new ECSNavAgentPathfinder
            //{
            //    status = AgentStatus.Idle,
            //    isActive = false
            //});
            ECSPathQuerySystem.SchedulePath(id, target.position, 1);
        }

        private void Update()
        {
            
            //RequestPath(new Vector3(random(), 0, random()));
            //ECSPathQuerySystem.SchedulePath(id, target.position, 1);
            //for (int i = 0; i < 100; i++)
            //{
            //    ECSPathQuerySystem.SchedulePath(id,
            //        new Vector3(random(), 0, random()));
            //}
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            var buffer = entityManager.GetBuffer<ECSPathBuffer>(id.Entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                Gizmos.DrawSphere(buffer[i].position, .25f);
                if (i + 1 < buffer.Length)
                {
                    Gizmos.DrawLine(buffer[i].position, buffer[i + 1].position);
                }
            }
        }
    }
    public class ECSPathMeshSystem : ECSManagerEntity<ECSPathMeshSystem>
    {
        public float3 Center = new float3(0, 0, 0);
        public float3 Size = new float3(80, 20, 80);

        private NavMeshData m_NavMesh;
        private NavMeshDataInstance m_NavMeshData;

        private TransformAccessArray m_TransformArray;
        private Dictionary<int, NavMeshBuildSource> m_Obstacles;

        public static void AddObstacle(Object obj, int areaMask = 0)
        {
            NavMeshBuildSource source;
            if (obj is MeshFilter mesh)
            {
                source = new NavMeshBuildSource()
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mesh.sharedMesh,
                    transform = mesh.transform.localToWorldMatrix,
                    area = areaMask
                };
                p_Instance.m_TransformArray.Add(mesh.transform);
            }
            else if (obj is Terrain terrain)
            {
                source = new NavMeshBuildSource()
                {
                    shape = NavMeshBuildSourceShape.Terrain,
                    sourceObject = terrain.terrainData,
                    transform = Matrix4x4.TRS(terrain.transform.position, Quaternion.identity, Vector3.one),
                    area = areaMask
                };
                p_Instance.m_TransformArray.Add(terrain.transform);
            }
            else throw new CoreSystemException(CoreSystemExceptionFlag.ECS, "NavMesh Obstacle 지정은 MeshFilter 혹은 Terrain만 가능합니다");

            p_Instance.m_Obstacles.Add(obj.GetInstanceID(), source);
        }
        public static void RemoveObstacle(int id)
        {
            p_Instance.m_Obstacles.Remove(id);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_NavMesh = new NavMeshData();
            m_NavMeshData = NavMesh.AddNavMeshData(m_NavMesh);

            m_TransformArray = new TransformAccessArray(256);
            m_Obstacles = new Dictionary<int, NavMeshBuildSource>();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_TransformArray.Dispose();
            //m_Obstacles.Dispose();
        }
        protected override void OnUpdate()
        {
            NavMeshBuildSettings defaultBuildSettings = NavMesh.GetSettingsByID(0);
            Bounds bounds = QuantizedBounds();
            List<NavMeshBuildSource> sources = m_Obstacles.Values.ToList();
            NavMeshBuilder.UpdateNavMeshDataAsync(m_NavMesh, defaultBuildSettings, sources, bounds);
        }

        private static float3 Quantize(float3 v, float3 quant)
        {
            float x = quant.x * math.floor(v.x / quant.x);
            float y = quant.y * math.floor(v.y / quant.y);
            float z = quant.z * math.floor(v.z / quant.z);
            return new float3(x, y, z);
        }
        Bounds QuantizedBounds()
        {
            // Quantize the bounds to update only when theres a 10% change in size
            
            return new Bounds(Quantize(Center, 0.1f * Size), Size);
        }

    }
}

#endif