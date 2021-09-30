namespace Syadeu.Presentation
{
    public struct PresentationLoop
    {
        public struct PresentationPreUpdate { }

        public struct PresentationBeforeUpdate { }
        public struct PresentationOnUpdate { }
        public struct PresentationAfterUpdate { }

        public struct PresentationLateUpdate
        {
            public struct TransformUpdate { }
            public struct AfterTransformUpdate { }
        }

        public struct PresentationPostUpdate { }
    }
}
