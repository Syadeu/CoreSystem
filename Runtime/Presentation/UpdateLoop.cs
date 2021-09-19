namespace Syadeu.Presentation
{
    public enum UpdateLoop
    {
        Default,

        /// <summary>
        /// <see cref="UnityEngine.PlayerLoop.PostLateUpdate"/>
        /// </summary>
        Transform,
        AfterTransform,
    }
}
