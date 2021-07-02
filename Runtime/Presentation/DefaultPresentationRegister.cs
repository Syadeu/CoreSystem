//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif


namespace Syadeu.Presentation
{
    internal sealed class DefaultPresentationRegister : IPresentationRegister
    {
        public void Register()
        {
            PresentationManager.RegisterSystem(new ScenePresentationSystem());
            //PresentationManager.RegisterSystem(new TestSystem());
            //PresentationManager.RegisterSystem(new Test123System());
        }
    }
}
