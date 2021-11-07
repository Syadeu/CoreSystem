#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif


using UnityEngine;
using UnityEngine.SubsystemsImplementation;

namespace Syadeu.Presentation
{
    public sealed class GameObjectSystem : PresentationSystemEntity<GameObjectSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        //protected override PresentationResult OnInitialize()
        //{
        //    SubsystemDescriptorWithProvider<

        //    return base.OnInitialize();
        //}

        //private class Ttesasdasdt : SubsystemWithProvider
        //{
        //    internal override SubsystemDescriptorWithProvider descriptor => throw new System.NotImplementedException();

        //    protected override void OnDestroy()
        //    {
        //        throw new System.NotImplementedException();
        //    }

        //    protected override void OnStart()
        //    {
        //        throw new System.NotImplementedException();
        //    }

        //    protected override void OnStop()
        //    {
        //        throw new System.NotImplementedException();
        //    }

        //    internal override void Initialize(SubsystemDescriptorWithProvider descriptor, SubsystemProvider subsystemProvider)
        //    {
        //        throw new System.NotImplementedException();
        //    }

        //}
    }
}
