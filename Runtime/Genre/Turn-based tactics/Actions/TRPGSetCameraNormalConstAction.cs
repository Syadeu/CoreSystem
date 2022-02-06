#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Render;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Camera/TRPG/Set Camera Normal")]
    [Guid("11593802-1260-4B3D-8CC1-F3977A8B2DD5")]
    internal sealed class TRPGSetCameraNormalConstAction : ConstAction<int>
    {
        [JsonIgnore]
        private RenderSystem m_RenderSystem;
        [JsonIgnore]
        private TRPGCameraMovement m_TRPGCameraMovement;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_RenderSystem = null;
        }

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        protected override int Execute()
        {
            if (m_TRPGCameraMovement == null)
            {
                m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();
            }

            m_TRPGCameraMovement.SetNormal();

            return 0;
        }
    }
}