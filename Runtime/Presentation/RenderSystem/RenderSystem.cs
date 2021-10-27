#undef UNITY_ADDRESSABLES

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

using Syadeu.Collections;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

#if CORESYSTEM_URP
using UnityEngine.Rendering.Universal;
#elif CORESYSTEM_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Syadeu.Presentation.Render
{
    [RequireGlobalConfig("Graphics")]
    public sealed class RenderSystem : PresentationSystemEntity<RenderSystem>
#if CORESYSTEM_HDRP
        , INotifySystemModule<RenderProjectionModule>
#endif
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;
        public override bool EnableTransformPresentation => true;

#if CORESYSTEM_URP
        public const string s_DefaultShaderName = "Universal Render Pipeline/Lit";
#elif CORESYSTEM_HDRP
        public const string s_DefaultShaderName = "HDRP/Lit";
#else
        public const string s_DefaultShaderName = "Standard";
#endif

        public static readonly LayerMask ProjectionLayer = LayerMask.NameToLayer("FloorProjection");
        public static readonly LayerMask ProjectionMask = LayerMask.GetMask("FloorProjection");

        private ObClass<Camera> m_Camera;
        private CameraComponent m_CameraComponent = null;
#if CORESYSTEM_URP
        private UniversalAdditionalCameraData m_CameraData;
        private readonly List<Camera> m_UICameras = new List<Camera>();
#endif
        private ObClass<Light> m_DirectionalLight;
        private Matrix4x4 m_Matrix4x4;

        [ConfigValue(Header = "Resolution", Name = "X")] private int m_ResolutionX;
        [ConfigValue(Header = "Resolution", Name = "Y")] private int m_ResolutionY;

        public Camera Camera
        {
            get => m_Camera.Value;
            set
            {
                m_Camera.Value = value;
            }
        }
        public CameraComponent CameraComponent => m_CameraComponent;
        public Light DirectionalLight
        {
            get => m_DirectionalLight.Value;
            set
            {
                if (value.type != UnityEngine.LightType.Directional)
                {
                    CoreSystem.Logger.LogError(Channel.Render,
                        $"{value.name} is not a Directional Light.");
                    return;
                }

                m_DirectionalLight.Value = value;
            }
        }
		public CameraFrustum.ReadOnly Frustum => GetFrustum();

        public event Action<Camera, Camera> OnCameraChanged;
        public event Action<ScriptableRenderContext, Camera> OnRender;

        private JobHandle m_FrustumJob;
		private CameraFrustum m_CameraFrustum;
        private CameraData m_LastCameraData;

        private LightData m_LastDirectionalLightData;

        public CameraData LastCameraData => m_LastCameraData;
        public LightData LastDirectionalLightData => m_LastDirectionalLightData;
        
        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_CameraFrustum = new CameraFrustum(new CameraData());
            m_LastCameraData = new CameraData() { orientation = quaternion.identity };

            m_Camera = new ObClass<Camera>(ObValueDetection.Changed);
            m_Camera.OnValueChange += OnCameraChangedHandler;

            //CoreSystem.Instance.OnRender -= Instance_OnRender;
            //CoreSystem.Instance.OnRender += Instance_OnRender;

            RenderPipelineManager.beginCameraRendering -= Instance_OnRender;
            RenderPipelineManager.beginCameraRendering += Instance_OnRender;

            m_DirectionalLight = new ObClass<Light>(ObValueDetection.Changed);
            m_LastDirectionalLightData = new LightData() { orientation = quaternion.identity };

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_Camera.OnValueChange -= OnCameraChangedHandler;
            //CoreSystem.Instance.OnRender -= Instance_OnRender;
            RenderPipelineManager.beginCameraRendering -= Instance_OnRender;

            m_CameraFrustum.Dispose();
        }
        private void OnCameraChangedHandler(Camera from, Camera to)
        {
            OnCameraChanged?.Invoke(from, to);
            if (to == null)
            {
                m_CameraComponent = null;
                return;
            }

            m_CameraComponent = to.GetComponent<CameraComponent>();
            m_Matrix4x4 = GetCameraMatrix4X4(to);
#if CORESYSTEM_URP
            m_CameraData = Camera.GetUniversalAdditionalCameraData();

            for (int i = m_UICameras.Count - 1; i >= 0; i--)
            {
                if (m_UICameras[i] == null)
                {
                    m_UICameras.RemoveAt(i);
                    continue;
                }

                m_CameraData.cameraStack.Add(m_UICameras[i]);
            }
#endif
        }
        private void Instance_OnRender(ScriptableRenderContext ctx, Camera cam)
        {
#if CORESYSTEM_SHAPES
            using (Shapes.Draw.Command(cam))
#endif
            {
                OnRender?.Invoke(ctx, cam);
            }
        }

        protected override PresentationResult BeforePresentation()
        {
            if (m_Camera.Value == null)
            {
                m_Camera.Value = Camera.main;
                //if (Camera == null) return PresentationResult.Warning("Cam not found");
                if (Camera == null) return base.BeforePresentation();
            }
            m_Matrix4x4 = GetCameraMatrix4X4(m_Camera.Value);

			//m_CameraFrustum.ScheduleUpdate(new CameraData(Camera));
			//m_CameraFrustum.Update(Camera);

			return base.BeforePresentation();
        }
        protected override PresentationResult TransformPresentation()
        {
            if (DirectionalLight != null)
            {
                m_LastDirectionalLightData.Update(DirectionalLight);

                //float4x4 matrix = m_LastDirectionalLightData.lightSpace;
                
            }

            if (Camera == null) return base.TransformPresentation();

            m_LastCameraData.Update(Camera);
            FrustumJob job = new FrustumJob
            {
                m_Frustum = m_CameraFrustum,
                m_Data = m_LastCameraData
            };
            m_FrustumJob = ScheduleAt(JobPosition.After, job);

			return base.TransformPresentation();
        }

        private struct FrustumJob : IJob
        {
            public CameraFrustum m_Frustum;
            public CameraData m_Data;

            public void Execute()
            {
                if (m_Data.orthographic)
                {
                    CameraFrustum.UpdateOrtho(
                        ref m_Frustum,
                        m_Data.position,
                        m_Data.orientation,
                        m_Data.orthographicSize,
                        m_Data.nearClipPlane,
                        m_Data.farClipPlane,
                        m_Data.aspect);
                }
                else
                {
                    CameraFrustum.UpdatePers(
                        ref m_Frustum, 
                        m_Data.position, 
                        m_Data.orientation,
                        m_Data.fov, 
                        m_Data.nearClipPlane,
                        m_Data.farClipPlane, 
                        m_Data.aspect);
                }
            }
        }

        #endregion

        public CameraFrustum.ReadOnly GetFrustum()
        {
            m_FrustumJob.Complete();
            return m_CameraFrustum.AsReadOnly();
        }
		internal CameraFrustum GetRawFrustum() => m_CameraFrustum;

#if CORESYSTEM_URP
        public void AddUICamera(Camera camera)
        {
            camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

            m_UICameras.Add(camera);
            if (m_CameraData != null) m_CameraData.cameraStack.Add(camera);
        }
        public void RemoveUICamera(Camera camera)
        {
            m_UICameras.Remove(camera);
            if (m_CameraData != null) m_CameraData.cameraStack.Remove(camera);
        }
#endif

        //private float4x4 GetCameraWorldMatrix()
        //{
        //    float4x4 projection = m_LastCameraData.projectionMatrix;
        //    float4x4 tr = new float4x4(new float3x3(m_LastCameraData.orientation), m_LastCameraData.position);

        //    return math.inverse(math.mul(projection, math.fastinverse(tr)));
        //}

#if CORESYSTEM_HDRP
        public ProjectionCamera GetProjectionCamera(Material mat, RenderTexture renderTexture)
        {
            return GetModule<RenderProjectionModule>().GetProjectionCamera(mat, renderTexture);
        }
#endif

        public float3 WorldToViewportPoint(float3 worldPoint)
        {
            float4x4 vp = math.mul(m_LastCameraData.projectionMatrix, m_LastCameraData.worldToLocalMatrix);

            float4 temp = math.mul(vp, new float4(worldPoint, 1));
            float3 point = temp.xyz / -temp.w;
            point.x += .5f;
            point.y += .5f;
            return point;
        }
        public float3 ScreenToWorldPoint(float3 screenPoint)
        {
            screenPoint.z = LastCameraData.nearClipPlane;
            return m_Camera.Value.ScreenToWorldPoint(screenPoint);
        }
        //public float3 ScreenToWorldPoint(float3 screenPoint)
        //{
        //    float4x4 localToWorld = new float4x4(new float3x3(m_LastCameraData.orientation), m_LastCameraData.position);
        //    float4x4 vp = math.inverse(math.mul(m_LastCameraData.projectionMatrix, math.fastinverse(localToWorld)));

        //    float4 temp = new float4
        //    {
        //        x = -((2 * screenPoint.x) / m_LastCameraData.pixelWidth - 1),
        //        y = -((2 * screenPoint.y) / m_LastCameraData.pixelHeight - 1),
        //        z = m_LastCameraData.nearClipPlane - .05f,
        //        w = 1
        //    };

        //    float4 pos = math.mul(vp, temp);

        //    //float3 campos = m_LastCameraData.position;
        //    //float3 dir = campos - pos.xyz;
        //    //float3 forward = math.mul(m_LastCameraData.orientation, new float3(0, 0, 1));

        //    //float distToPlane = math.dot(dir, forward);
        //    //if (math.abs(distToPlane) >= math.EPSILON)
        //    //{
        //    //    if (!m_LastCameraData.orthographic)
        //    //    {
        //    //        dir *= screenPoint.z / distToPlane;
        //    //        return campos + dir;
        //    //    }

        //    //    return pos.xyz - forward * (distToPlane - screenPoint.z);
        //    //}

        //    //return float3.zero;
        //    pos.w = 1 / pos.w;
        //    pos.x *= pos.w; pos.y *= pos.w; pos.z *= pos.w;
        //    return pos.xyz;
        //}
        //public float3 ViewportToScreenPoint(float3 viewportPoint)
        //{
        //    $"{viewportPoint} , {m_LastCameraData.pixelWidth} : {m_LastCameraData.pixelHeight}".ToLog();
        //    return new float3(
        //        viewportPoint.x * m_LastCameraData.pixelWidth,
        //        viewportPoint.y * m_LastCameraData.pixelHeight,
        //        //viewportPoint.z
        //        LastCameraData.nearClipPlane
        //        );
        //}

        //public float3 WorldToScreenPoint(float3 worldPoint) => ViewportToScreenPoint(WorldToViewportPoint(worldPoint));
        public float3 WorldToScreenPoint(float3 worldPoint) => m_Camera.Value.WorldToScreenPoint(worldPoint);
        //public float3 WorldToScreenPoint(float3 worldPoint)
        //{
        //    float4x4 vp = math.mul(LastCameraData.projectionMatrix, LastCameraData.worldToLocalMatrix);
        //    float4 temp = math.mul(vp, new float4(worldPoint, 1));

        //    if (temp.w.Equals(0)) return float3.zero;

        //    temp.x = (temp.x / temp.w + 1) * .5f * LastCameraData.pixelWidth;
        //    temp.y = (temp.y / temp.w + 1) * .5f * LastCameraData.pixelHeight;
        //    return new float3(temp.xy, worldPoint.z);
        //}

        #region Ray

        //float4x4 GetWorldToCameraMatrix()
        //{
        //    float4x4 m = float4x4.Scale(new Vector3(1.0F, 1.0F, -1.0F));
        //    m = m * GetCameraWorldMatrix();
        //    return m;
        //}
        //float4x4 GetProjectionMatrix() => m_LastCameraData.projectionMatrix;
        //float4x4 GetWorldToClipMatrix()
        //{
        //    float4x4 t = GetProjectionMatrix() * GetWorldToCameraMatrix();
        //    return t;
        //}
        //float4x4 GetCameraToWorldMatrix()
        //{
        //    float4x4 m = GetWorldToCameraMatrix();
        //    return math.inverse(m);
        //}
        //bool CameraUnProject(bool ortho, float3 p, float4x4 cameraToWorld, float4x4 clipToWorld, Rect viewport, out float3 outP)
        //{
        //    // pixels to -1..1
        //    float3 in_v;
        //    in_v.x = (p.x - viewport.x) * 2.0f / viewport.width - 1.0f;
        //    in_v.y = (p.y - viewport.y) * 2.0f / viewport.height - 1.0f;
        //    // It does not matter where the point we unproject lies in depth; so we choose 0.95, which
        //    // is further than near plane and closer than far plane, for precision reasons.
        //    // In a perspective camera setup (near=0.1, far=1000), a point at 0.95 projected depth is about
        //    // 5 units from the camera.
        //    in_v.z = 0.95f;

        //    float3 pointOnPlane = math.mul(clipToWorld, new float4(in_v, 1)).xyz;
        //    //if (clipToWorld.PerspectiveMultiplyPoint3(in_v, out pointOnPlane))
        //    {
        //        // Now we have a point on the plane perpendicular to the viewing direction. We need to return the one that is on the line
        //        // towards this point, and at p.z distance along camera's viewing axis.
        //        float3 cameraPos = cameraToWorld.c3.xyz;
        //        float3 dir = pointOnPlane - cameraPos;

        //        // The camera/projection matrices follow OpenGL convention: positive Z is towards the viewer.
        //        // So negate it to get into Unity convention.
        //        float3 forward = -cameraToWorld.c2.xyz;
        //        float distToPlane = Vector3.Dot(dir, forward);
        //        if (Mathf.Abs(distToPlane) >= 1.0e-6f)
        //        {
        //            //bool isPerspective = clipToWorld.IsPerspective();
        //            if (!ortho)
        //            {
        //                dir *= p.z / distToPlane;
        //                outP = cameraPos + dir;
        //            }
        //            else
        //            {
        //                outP = pointOnPlane - forward * (distToPlane - p.z);
        //            }
        //            return true;
        //        }
        //    }
        //    outP = new Vector3(0.0f, 0.0f, 0.0f);
        //    return false;
        //}

        //public Ray ScreenPointToRay(float2 viewPortPos)
        //{
        //    Rect viewport = new Rect(0, 0, Screen.width, Screen.height);
        //    Ray ray = new Ray();
        //    float3 o;
        //    float4x4 clipToWorld = GetCameraWorldMatrix();

        //    float4x4 camToWorld = GetCameraToWorldMatrix();
        //    if (!CameraUnProject(m_LastCameraData.orthographic, new Vector3(viewPortPos.x, viewPortPos.y, m_LastCameraData.nearClipPlane), camToWorld, clipToWorld, viewport, out o))
        //    {
        //        return new Ray(m_LastCameraData.position, new Vector3(0, 0, 1));
        //    }
        //    ray.origin = o;



        //    if (m_LastCameraData.orthographic)
        //    {
        //        // In orthographic projection we get better precision by circumventing the whole projection and subtraction.
        //        //ray.direction = Vector3.Normalize(-camToWorld.GetAxisZ());

        //        float3 forward = new float3(0, 0, 1);
        //        ray.direction = Vector3.Normalize(-camToWorld.c2.xyz);
        //    }
        //    else
        //    {
        //        // We need to sample a point much further out than the near clip plane to ensure decimals in the ray direction
        //        // don't get lost when subtracting the ray origin position.
        //        if (!CameraUnProject(m_LastCameraData.orthographic, new Vector3(viewPortPos.x, viewPortPos.y, m_LastCameraData.nearClipPlane + 1000), camToWorld, clipToWorld, viewport, out o))
        //        {
        //            return new Ray(m_LastCameraData.position, Vector3.forward);
        //        }
        //        float3 dir = o - (float3)ray.origin;
        //        ray.direction = (Vector3.Normalize(dir));
        //    }
        //    return ray;
        //}
        //public Ray ViewportPointToRay(Vector3 position)
        //{
        //    return ScreenPointToRay(new Vector2(position.x * Screen.width, position.y * Screen.height));
        //}
        //public Ray ScreenPointToRay(float3 position)
        //{
        //    return ViewportPointToRay(new float3(position.x / Screen.width, position.y / Screen.height, position.z));
        //}

        #endregion

        //public Ray ScreenPointToRay(float3 screenPoint)
        //{
        //    float3 pos = ScreenToWorldPoint(screenPoint);
        //    return new Ray(m_LastCameraData.position, math.normalize(pos - m_LastCameraData.position));
        //}
        public Ray ScreenPointToRay(float3 screenPoint)
        {
            return m_Camera.Value.ScreenPointToRay(screenPoint);
        }

        public void SetResolution()
        {
            //Screen.SetResolution(100,100, FullScreenMode.ExclusiveFullScreen, )
        }

        #region Legacy

        /// <summary>
        /// 해당 월드 좌표를 입력한 Matrix 기반으로 2D 좌표값을 반환합니다.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static float3 GetScreenPoint(float4x4 matrix, float3 worldPosition)
        {
            float4 p4 = new float4(worldPosition, 1);
            float4 result4 = math.mul(matrix, p4);
            float3 screenPoint = result4.xyz;
            screenPoint /= -result4.w;
            screenPoint.x = screenPoint.x / 2 + 0.5f;
            screenPoint.y = screenPoint.y / 2 + 0.5f;
            screenPoint.z = -result4.w;

            return screenPoint;
        }
        internal static float4x4 GetCameraMatrix4X4(Camera cam) => cam.projectionMatrix * cam.transform.worldToLocalMatrix;
        public bool IsInCameraScreen(Vector3 worldPosition, float3 offset = default)
        {
            return IsInCameraScreen(worldPosition, m_Matrix4x4, offset);
        }
        public bool IsInCameraScreen(float3[] worldVertices, float3 offset = default)
        {
            return IsInCameraScreen(worldVertices, m_Matrix4x4, offset);
        }
        public bool IsInCameraScreen(NativeArray<float3> worldVertices, float3 offset = default)
        {
            return IsInCameraScreen(worldVertices, m_Matrix4x4, offset);
        }
        /// <summary>
        /// 해당 좌표가 입력한 카메라 내부에 위치하는지 반환합니다.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static bool IsInCameraScreen(Camera cam, float3 worldPosition)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
            screenPos.y = Screen.height - screenPos.y;

            if (screenPos.y < 0 || screenPos.y > Screen.height ||
                screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                return false;
            }
            return true;
        }
        internal static bool IsInCameraScreen(float3 worldPosition, float4x4 matrix, float3 offset)
        {
            Vector3 screenPoint = GetScreenPoint(matrix, worldPosition);
            
            return screenPoint.z > 0 - offset.z &&
                screenPoint.x > 0 - offset.x &&
                screenPoint.x < 1 + offset.x &&
                screenPoint.y > 0 - offset.y &&
                screenPoint.y < 1 + offset.y;
        }
        internal static bool IsInCameraScreen(float3[] vertices, float4x4 matrix, float3 offset)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                if (IsInCameraScreen(vertices[i], matrix, offset)) return true;
            }
            return false;
        }
        internal static bool IsInCameraScreen(NativeArray<float3> vertices, float4x4 matrix, Vector3 offset)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                if (IsInCameraScreen(vertices[i], matrix, offset)) return true;
            }
            return false;
        }

        #endregion
    }

#if CORESYSTEM_HDRP
    internal sealed class RenderProjectionModule : PresentationSystemModule<RenderSystem>
    {
        private int m_Creation = 0;
        private readonly Stack<Camera> m_UnusedProjectionCameras = new Stack<Camera>();

        private SceneSystem m_SceneSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_SceneSystem = null;
        }
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
        }

        protected override void TransformPresentation()
        {

        }

        public ProjectionCamera GetProjectionCamera(Material mat, RenderTexture renderTexture)
        {
            Camera cam;
            DecalProjector projector;

            if (m_UnusedProjectionCameras.Count > 0)
            {
                cam = m_UnusedProjectionCameras.Pop();
                cam.gameObject.SetActive(true);

                projector = cam.GetComponentInChildren<DecalProjector>();
            }
            else
            {
                GameObject gameObj = m_SceneSystem.CreateGameObject($"Projection Camera {m_Creation++}");
                cam = gameObj.AddComponent<Camera>();
                cam.orthographic = true;

                cam.cullingMask = RenderSystem.ProjectionMask;
                gameObj.AddComponent<HDAdditionalCameraData>().volumeLayerMask = RenderSystem.ProjectionMask;

                cam.orthographicSize = 20;

                cam.transform.eulerAngles = new Vector3(90, 0, 0);
                cam.transform.position = new Vector3(0, 10, 0);

                GameObject projectorObj = new GameObject("Projector");
                projectorObj.transform.SetParent(gameObj.transform);
                projectorObj.transform.localEulerAngles = new Vector3(0, 0, 0);
                projectorObj.transform.localPosition = Vector3.zero;
                projectorObj.transform.localScale = Vector3.zero;

                projector = projectorObj.AddComponent<DecalProjector>();

                projector.size = new Vector3(40, 40, 30);
            }

            cam.targetTexture = renderTexture;
            projector.material = mat;

            var main = new ProjectionCamera(this, cam, projector);
            return main;
        }
        internal void ReserveProjectionCamere(ProjectionCamera main, Camera cam)
        {
            cam.gameObject.SetActive(false);
            m_UnusedProjectionCameras.Push(cam);
        }
    }
#endif

#if CORESYSTEM_HDRP
    public sealed class ProjectionCamera : IDisposable
    {
        private RenderProjectionModule m_Module;
        
        private Camera m_Camera;
        private Transform m_Transform;
        private DecalProjector m_Projector;

        public RenderTexture RenderTexture
        {
            get => m_Camera.targetTexture;
            //set => m_Camera.targetTexture = value;
        }

        private ProjectionCamera() { }
        internal ProjectionCamera(RenderProjectionModule module, Camera cam, DecalProjector projector
            )
        {
            m_Module = module;
            m_Camera = cam;
            m_Transform = cam.transform;
            m_Projector = projector;
        }

        public void SetPosition(Vector3 pos)
        {
            m_Transform.position = pos + new Vector3(0, 10, 0);
        }
        public void Dispose()
        {
            m_Module.ReserveProjectionCamere(this, m_Camera);

            m_Module = null;
            m_Camera = null;
            m_Transform = null;
            m_Projector = null;
        }
    }
#endif
}
