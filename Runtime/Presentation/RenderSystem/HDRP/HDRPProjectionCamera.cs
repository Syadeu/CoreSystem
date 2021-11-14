using System;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
#if CORESYSTEM_HDRP
    using UnityEngine.Rendering.HighDefinition;

    public sealed class HDRPProjectionCamera : IDisposable
    {
        private HDRPRenderProjectionModule m_Module;
        
        private Camera m_Camera;
        private Transform m_Transform;
        private DecalProjector m_Projector;

        public RenderTexture RenderTexture
        {
            get => m_Camera.targetTexture;
            //set => m_Camera.targetTexture = value;
        }

        private HDRPProjectionCamera() { }
        internal HDRPProjectionCamera(HDRPRenderProjectionModule module, Camera cam, DecalProjector projector
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
