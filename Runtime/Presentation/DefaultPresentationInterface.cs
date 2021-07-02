//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif


namespace Syadeu.Presentation
{
    [StaticManagerIntializeOnLoad]
    internal sealed class DefaultPresentationInterface : StaticDataManager<DefaultPresentationInterface>, IPresentationRegister
    {
        public override void OnStart()
        {
            PresentationManager.StartPresentation();
        }

        public void Register()
        {
            PresentationManager.RegisterSystem(new ScenePresentationSystem());
            //PresentationManager.RegisterSystem(new TestSystem());
            //PresentationManager.RegisterSystem(new Test123System());
        }
    }
}
