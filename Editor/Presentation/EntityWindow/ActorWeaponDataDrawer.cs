using Syadeu;
using Syadeu.Presentation;
using Syadeu.Presentation.Actor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ActorWeaponDataDrawer : ObjectBaseDrawer<ActorWeaponData>
    {
        private ActorWeaponPreviewScene m_PreviewScene;

        public ActorWeaponDataDrawer(ObjectBase weaponData) : base(weaponData)
        {

        }

        protected override void DrawGUI()
        {
            DrawHeader();
            DrawDescription();

            if (GUILayout.Button("Open"))
            {
                m_PreviewScene = new ActorWeaponPreviewScene(TargetObject);
                StageUtility.GoToStage(m_PreviewScene, true);
            }
            if (GUILayout.Button("close"))
            {
                StageUtility.GoToMainStage();
            }
        }
    }

    public sealed class ActorWeaponPreviewScene : PreviewSceneStage
    {
        private GUIContent m_Header;
        private ActorWeaponData m_TargetWeaponData;

        public ActorWeaponPreviewScene(ActorWeaponData weaponData)
        {
            m_Header = new GUIContent($"{weaponData.Name} Preview");
            m_TargetWeaponData = weaponData;
        }
        protected override GUIContent CreateHeaderContent() => m_Header;

        protected override bool OnOpenStage()
        {
            "in".ToLog();
            return base.OnOpenStage();
        }
    }
}
